namespace Lumper.UI.Updater;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.BSP.IO;
using Lumper.UI.Views;
using Newtonsoft.Json;
using NLog;

public sealed partial class Updater
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private const float DownloadProgressPercentage = 80;

    /// <summary>
    /// record for deserializing JSON objects
    /// given in the GitHub JSON API response
    /// </summary>
    private record Asset
    {
        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set;  }

        [JsonProperty("name")]
        public string Name { get; set;  }
    }
    /// <summary>
    ///  record for deserializing Github API response data
    ///  see https://api.github.com/repos/momentum-mod/lumper/releases for the full format
    /// </summary>

    private record GithubUpdate
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set;  }

        [JsonProperty("assets")]
        public Asset[] Assets { get; set;  }

    }
    /// <summary>
    /// Major/Minor/Patch format
    /// </summary>
    /// <param name="major">First digits of the version format</param>
    /// <param name="minor">Second digits of the version format</param>
    /// <param name="patch">Third digits of the version format</param>
    public sealed record LumperVersion(int major, int minor, int patch)
    {
        public int Major { get; } = major;
        public int Minor { get; } = minor;
        public int Patch { get; } = patch;

        public override string ToString() => $"{major}.{minor}.{patch}";
    }

    [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)")]
    private static partial Regex VersionRegex();

    /// <summary>
    /// Parses a string to find the major/minor/patch versioning
    /// </summary>
    /// <param name="s">String containing a version number following the format 'xx.yy.zz'</param>
    private static LumperVersion ParseVersion(string s)
    {
        //match pattern of xx.yy.zz
        Match match = VersionRegex().Match(s);
        if (!match.Success)
            throw new InvalidDataException("Could not parse Major/Minor/Patch version.");

        GroupCollection currentVersion = match.Groups;

        return new LumperVersion(
            int.Parse(currentVersion[1].ToString()),
            int.Parse(currentVersion[2].ToString()),
            int.Parse(currentVersion[3].ToString()));
    }

    /// <summary>
    /// Runs the command line (windows) or shell (linux) and passes a command to it.
    /// </summary>
    /// <param name="command"></param>
    private static void ExecuteCommand(string command)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(
                new ProcessStartInfo("cmd.exe", "/c " + command)
                    { CreateNoWindow = true,
                      UseShellExecute = false,
                      RedirectStandardError = true,
                      RedirectStandardOutput = true,
                      WorkingDirectory = Directory.GetCurrentDirectory()
                    });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start(
                new ProcessStartInfo(command)
                    { CreateNoWindow = true,
                      UseShellExecute = true,
                      RedirectStandardError = true,
                      RedirectStandardOutput = true,
                      WorkingDirectory = Directory.GetCurrentDirectory()
            });
        }
        else
        {
            throw new InvalidProgramException();
        }
    }

    /// <summary>
    /// Grab the JSON from the Github API and deserializes it
    /// </summary>
    private static async Task<GithubUpdate> FetchGithubUpdates()
    {
        var client = new HttpClient();

        var request = new HttpRequestMessage() {
            RequestUri = new Uri("https://api.github.com/repos/momentum-mod/lumper/releases"),
            Method = HttpMethod.Get
        };
        client.DefaultRequestHeaders.Add("User-Agent", "Other");
        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Could not connect to Github API.",null,response.StatusCode);

        var jsonString = await response.Content.ReadAsStringAsync();
        var nodes = JsonNode.Parse(jsonString);
        if (nodes?[0] is not {} latestNode)
            throw new InvalidDataException("Failed to parse version JSON");

        GithubUpdate? latestUpdate = JsonConvert.DeserializeObject<GithubUpdate>(latestNode.ToString());
        if (latestUpdate is null)
            throw new InvalidDataException("Failed to parse version JSON");

        return latestUpdate;
    }

    /// <summary>
    /// Checks for possible update on the Github releases page by the tag name
    /// </summary>
    /// <returns>A tuple where the first value defines if there is an update ready, and the second value is the latest Version</returns>
    public static async Task<(bool, LumperVersion)> CheckForUpdate()
    {
        GithubUpdate assets = await FetchGithubUpdates();

        // Parse tag name to find the current and latest version
        // Finding the format of xx.yy.zz
        System.Version? assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;
        if (assemblyVersion is null)
            throw new ApplicationException("Unknown assembly version");

        LumperVersion current = ParseVersion(assemblyVersion.ToString());
        LumperVersion latest = ParseVersion(assets.TagName);

       return (current != latest, latest);
    }

    private const float DownloadProgressOverallProportion = 80;

    /// <summary>
    /// Download a file over HTTP from the given URL.
    /// </summary>
    /// <returns>True unless download was cancelled.</returns>
    private static async Task<bool> HttpDownload(string url, string fileName, IoHandler handler, CancellationTokenSource cts)
    {
        // The amount the progress bar should go up to while downloading - we still have to unzip for the other percentage
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(80 * 1024);

        if (File.Exists(fileName))
            File.Delete(fileName);

        var stream = new FileStream(fileName, FileMode.CreateNew);
        try
        {

            using var httpClient = new HttpClient();
            using HttpResponseMessage response =
                await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cts.Token);

            await using Stream downloadStream = await response.Content.ReadAsStreamAsync(cts.Token);

            if (response.Content.Headers.ContentLength is null)
            {
                handler.UpdateProgress(0, "Downloading (unknown length)");
                await downloadStream.CopyToAsync(stream, cts.Token);
            }
            else
            {
                int read;
                var length = (int)response.Content.Headers.ContentLength.Value;
                var remaining = length;
                while (!handler.Cancelled &&
                       (read = await downloadStream.ReadAsync(
                           buffer.AsMemory(0, int.Min(buffer.Length, remaining)),
                           cts.Token)) >
                       0)
                {
                    var prog = (float)read / length * DownloadProgressOverallProportion;
                    handler.UpdateProgress(prog, $"{float.Floor((1 - ((float)remaining / length)) * DownloadProgressOverallProportion)}%");
                    await stream.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                    remaining -= read;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex is TaskCanceledException)
                Logger.Info("Download cancelled by user");
            else
                Logger.Error(ex, "Failed to download!");

            await stream.DisposeAsync();
            return false;
        }
        finally
        {
            stream.Close();
        }

        if (handler.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Returns the URL to the download link for the OS-specific version.
    /// </summary>
    /// <returns></returns>
    private static string GetReleaseDownloadUrl(GithubUpdate assets, string osName)
    {
        foreach (Asset asset in assets.Assets)
        {
            if (asset.Name.Contains(osName, StringComparison.OrdinalIgnoreCase))
            {
                return asset.BrowserDownloadUrl;
            }
        }

        throw new InvalidDataException($"Could not find download link for {osName}");
    }

    /// <summary>
    /// Downloads an update for the program, applies it, and then restarts itself with the new version
    /// </summary>
    /// <returns></returns>
    public static async ValueTask Update()
    {
        try
        {
            GithubUpdate assets = await FetchGithubUpdates();
            LumperVersion latest = ParseVersion(assets.TagName);

            OSPlatform os;
            string osName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                os = OSPlatform.Windows;
                osName = "win";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                os = OSPlatform.Linux;
                osName = "linux";
            }
            else
            {
                throw new ApplicationException("OS is not supported");
            }

            var downloadUrl = GetReleaseDownloadUrl(assets, osName);
            var fileName = $"{osName}_{latest}.zip";
            var directoryName = $"{fileName}temp";
            var cts = new CancellationTokenSource();
            var handler = new IoHandler(cts);

            var progressWindow = new IoProgressWindow {
                Title = $"Downloading {downloadUrl}",
                Handler = handler
            };
            _ = progressWindow.ShowDialog(Program.Desktop.MainWindow);


            // Download and unzip to a temp directory
            var downloadSuccess = await HttpDownload(new Uri(downloadUrl).AbsoluteUri, fileName, handler, cts);
            if (!downloadSuccess)
            {
                Logger.Info("Update download was cancelled");
                return;
            }


            if (Directory.Exists(directoryName))
                Directory.Delete(directoryName, true);

            var progress = DownloadProgressPercentage / 100.0f;

            Directory.CreateDirectory(directoryName);
            using (ZipArchive archive = ZipFile.OpenRead(fileName))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    await using Stream zipStream = entry.Open();

                    var deltaProgress = 1 / (float)archive.Entries.Count * (100 - DownloadProgressPercentage);
                    progress += deltaProgress;
                    handler.UpdateProgress(deltaProgress, $"{float.Floor(DownloadProgressPercentage + progress)}%");

                    var f = new FileStream($"{directoryName}/{entry.Name}", FileMode.CreateNew);
                    await zipStream.CopyToAsync(f, cts.Token);
                    f.Close();
                }
            }

            var currentDirectory = Directory.GetCurrentDirectory();

            // Wait 2 seconds for the process to fully exit before
            // copying files from the temp directory to the root directory,
            // then delete the temp directory and run the program again
            var command =
                os == OSPlatform.Linux
                    ? $"""
                           sleep 2 &&
                           yes | cp -rf "{currentDirectory}\{directoryName}" &&
                           rm "{currentDirectory}\{fileName}" &&
                           rm -rf "{currentDirectory}\{directoryName}" &&
                           ./Lumper.UI
                       """
                    : $"""
                           sleep 2
                           && xcopy /s /Y "{currentDirectory}\{directoryName}"
                           && rm "{currentDirectory}\{fileName}"
                           && rmdir /s /q "{currentDirectory}\{directoryName}"
                           && Lumper.UI.exe
                      """;

            command = command.Replace(Environment.NewLine, " ").Replace("\n", " ");

            ExecuteCommand(command);

            //exit so we can overwrite the executable
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to process update!");
        }
    }
}
