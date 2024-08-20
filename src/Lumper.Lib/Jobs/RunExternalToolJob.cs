namespace Lumper.Lib.Jobs;

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.Bsp;
using Lumper.Lib.Bsp.Enum;
using Lumper.Lib.Bsp.IO;
using NLog;

public class RunExternalToolJob : Job, IJob
{
    public static string JobName => "Run External Tool";
    public override string JobNameInternal => JobName;

    public string? Path { get; set; }
    public string? Args { get; set; }
    public string? WorkingDir { get; set; }
    public bool WritesToInputFile { get; set; }
    public bool WritesToStdOut { get; set; }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private sealed class Output : IDisposable
    {
        public MemoryStream Mem { get; }
        private readonly BinaryWriter _writer;
        private readonly Stream _stream;
        private readonly byte[] _buffer;
        private readonly Logger _logger;
        private string _logString;

        public Output(Stream stream, string logName)
        {
            Mem = new MemoryStream();
            _writer = new BinaryWriter(Mem);
            _stream = stream;
            _buffer = new byte[80 * 1024];
            _logger = LogManager.GetLogger(logName);
            _logString = "";
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
                {
                    _writer.Write(_buffer, 0, read);

                    _logString += Encoding.UTF8.GetString(_buffer, 0, read);
                    var fuckWindows = _logString.Contains("\r\n");
                    var split = _logString.Split(fuckWindows ? "\r\n" : "\n");
                    foreach ((var line, var index) in split.Select((x, i) => (x, i)))
                    {
                        if (index == split.Length - 1)
                        {
                            _logString = line;
                            break;
                        }

                        _logger.Info(line.Replace("\n", ""));
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    public override bool Run(BspFile bsp)
    {
        if (WritesToStdOut && WritesToInputFile)
            throw new InvalidDataException("Can't use both input file and stdout");

        if (Path is null)
        {
            Logger.Warn("Missing Path, ignoring job.");
            return false;
        }

        // Don't have any sensible way of knowing how long process will take, just updating
        // progress incrementally as we do IO-bound tasks.
        Progress.Max = 100;
        Progress.Count = 0;

        var inputPath = System.IO.Path.GetTempFileName() + ".bsp";
        var outputPath = WritesToStdOut || WritesToInputFile ? null : System.IO.Path.GetTempFileName() + ".bsp";

        // TODO: It'd be nice to do full task cancellation, in which case we'd be passing a CT into this method.
        var handler = new IoHandler(new CancellationTokenSource());

        using (FileStream fileStream = File.Open(inputPath, FileMode.Create))
        {
            if (!bsp.SaveToStream(handler, fileStream, DesiredCompression.Unchanged))
            {
                Logger.Error("Failed to save BSP to temporary file, exiting");
                return false;
            }
        }

        Progress.Count = 25;

        Output stdOut,
            stdErr;
        bool ret;
        var args = Args;
        using (var process = new Process())
        {
            if (args is not null)
            {
                args = args.Replace("%INPUT%", $"\"{inputPath}\"");
                args = args.Replace("%DIR%", $"\"{WorkingDir}\"");
                if (!WritesToStdOut && !WritesToStdOut)
                    args = args.Replace("%OUTPUT%", $"\"{outputPath}\"");
            }

            process.StartInfo = new ProcessStartInfo
            {
                FileName = Path,
                Arguments = args,
                WorkingDirectory = WorkingDir,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var name = new FileInfo(Path).Name;
            Logger.Info($"Running {name} with args {args}");
            process.Start();

            stdOut = new Output(process.StandardOutput.BaseStream, name);
            stdErr = new Output(process.StandardError.BaseStream, name);

            Task.WaitAll(stdOut.Read(), stdErr.Read());
            ret = process.ExitCode == 0;
        }

        Progress.Count = 75;
        stdOut.Mem.Seek(0, SeekOrigin.Begin);
        stdErr.Mem.Seek(0, SeekOrigin.Begin);

        handler = new IoHandler(new CancellationTokenSource());
        if (ret)
        {
            if (WritesToStdOut)
            {
                bsp.Load(stdOut.Mem, handler);
            }
            else if (WritesToInputFile)
            {
                bsp.Load(inputPath, handler);
            }
            else
            {
                bsp.Load(outputPath!, handler);
            }
        }
        else
        {
            Logger.Error(
                $"{System.IO.Path.GetFileName(Path)} executable returned non-zero exit code!"
                    + "\nstderr:"
                    + new StreamReader(stdErr.Mem).ReadToEnd().Replace("\n", "\n       ")
            );
        }

        Progress.Count = Progress.Max;

        return ret;
    }
}
