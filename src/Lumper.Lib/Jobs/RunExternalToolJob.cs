namespace Lumper.Lib.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.BSP;

public class RunExternalToolJob(
    string? path,
    string? args,
    string? inputFile,
    string? outputFile,
    bool useStdOut = false)
    : LumperTask
{
    public override string Type => "RunExternalToolTask";
    public string? Path { get; set; } = path;
    public string? Args { get; set; } = args;
    // The current BSP will be saved to this and it should be the input for the command
    // Null if you don't want to save and override all previous changes
    public string? InputFile { get; set; } = inputFile;
    public string? OutputFile { get; set; } = outputFile;
    public bool UseStdOut { get; set; } = useStdOut;

    private sealed class Output : IDisposable
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
                var read = await _stream.ReadAsync(new Memory<byte>(_buffer));
                if (read > 0)
                    _writer.Write(_buffer, 0, read);
                else
                    break;
            }
        }
    }

    public override TaskResult Run(BspFile bsp)
    {
        if (InputFile is not null)
        {
            bsp.Save(InputFile);
            var fiIn = new FileInfo(InputFile);
            // Guessing based on input file length
            // Probably wrong but better than nothing (?)
            Progress.Max = fiIn.Length;
        }
        else
        {
            Console.WriteLine($"Warning: Inputfile not set for external command '{Path} {Args}'");
        }

        if (File.Exists(OutputFile))
        {
            // TODO: This is *probably* okay but maybe best to have a toggle button for this
            // behaviour, just in case it nukes a file someone actual cares about.
            Console.WriteLine("Warning: Output file exists, overwriting");
            File.Delete(OutputFile);
        }


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

            Task taskStdOut = stdOut.Read();
            Task taskStdErr = stdErr.Read();

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

            Task.WaitAll(taskStdOut, taskStdErr);
            ret = process.ExitCode == 0
                ? TaskResult.Success
                : TaskResult.Failed;
        }

        stdOut.Mem.Seek(0, SeekOrigin.Begin);
        stdErr.Mem.Seek(0, SeekOrigin.Begin);

        if (ret == TaskResult.Success)
        {
            if (UseStdOut)
                bsp.Load(stdOut.Mem);
            else
                bsp.Load(OutputFile);

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
