using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using Lumper.Lib.BSP;

namespace Lumper.Lib.Tasks
{
    public class RunExternalToolTask : LumperTask
    {
        public override string Type { get; } = "RunExternalToolTask";
        public string Path { get; set; }
        public string Args { get; set; }
        //The current BSP will be saved to this and it should be the input for the command
        //Null if you don't want to save and override all previous changes
        public string InputFile { get; set; }
        public string OutputFile { get; set; }
        public bool UseStdOut { get; set; }


        public RunExternalToolTask() { }
        public RunExternalToolTask(string path, string args)
        {
            Path = path;
            Args = args;
        }
        public RunExternalToolTask(string path, string args, string inputFile, string outputFile)
            : this(path, args)
        {
            InputFile = inputFile;
            OutputFile = outputFile;
            UseStdOut = false;
        }
        public RunExternalToolTask(string path, string args, string inputFile, bool useStdOut)
            : this(path, args)
        {
            InputFile = inputFile;
            OutputFile = null;
            UseStdOut = useStdOut;
        }
        private class Output : IDisposable
        {
            public MemoryStream Mem;
            private readonly BinaryWriter _writer;
            private readonly Stream _stream;
            private readonly byte[] _buffer;
            public Output(Stream stream)
            {

                Mem = new MemoryStream();
                _writer = new BinaryWriter(Mem);
                _stream = stream;
                _buffer = new byte[80 * 1024];
            }
            public void Dispose()
            {
                Mem.Dispose();
                _writer.Dispose();
                _stream.Dispose();
            }

            public async Task Read()
            {
                while (_stream.CanRead)
                {
                    int read = await _stream.ReadAsync(new Memory<byte>(_buffer));
                    if (read > 0)
                        _writer.Write(_buffer, 0, read);
                    else
                        break;
                }
            }
        }
        public override TaskResult Run(BspFile map)
        {
            if (InputFile is not null)
            {
                map.Save(InputFile);
                var fiIn = new FileInfo(InputFile);
                //guessing based on input file length 
                //probably wrong but better than nothing (?)
                Progress.Max = fiIn.Length;
            }
            else
                Console.WriteLine($"Warning: Inputfile not set for external command '{Path} {Args}'");

            if (File.Exists(OutputFile))
                File.Delete(OutputFile);


            var startInfo = new ProcessStartInfo()
            {
                FileName = Path,
                Arguments = Args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            Output stdOut;
            Output stdErr;
            TaskResult ret;
            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();

                stdOut = new Output(process.StandardOutput.BaseStream);
                stdErr = new Output(process.StandardError.BaseStream);

                var taskStdOut = stdOut.Read();
                var taskStdErr = stdErr.Read();

                while (!process.HasExited)
                {

                    if (UseStdOut)
                    {
                        Progress.Count = stdOut.Mem.Length;
                    }
                    else
                    {
                        var fiOut = new FileInfo(OutputFile);
                        if (fiOut.Exists)
                            Progress.Count = fiOut.Length;
                    }
                    Thread.Sleep(30);
                }

                System.Threading.Tasks.Task.WaitAll(taskStdOut, taskStdErr);
                ret = process.ExitCode == 0
                                 ? TaskResult.Success
                                 : TaskResult.Failed;
            }

            stdOut.Mem.Seek(0, SeekOrigin.Begin);
            stdErr.Mem.Seek(0, SeekOrigin.Begin);

            if (ret == TaskResult.Success)
            {
                if (UseStdOut)
                    map.Load(stdOut.Mem);
                else
                    map.Load(OutputFile);

                Progress.Count = Progress.Max;
            }
            else
            {
                MemoryStream stream;
                if (stdErr.Mem.Length > 0)
                    stream = stdErr.Mem;
                else
                    stream = stdOut.Mem;
                var r = new StreamReader(stream);
                Console.WriteLine($"{System.IO.Path.GetFileName(Path)} ERROR: {r.ReadToEnd()}");
            }
            return ret;
        }
    }
}