namespace Lumper.UI.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Lumper.Lib.Bsp.IO;
using Lumper.UI.Views;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Newtonsoft.Json;
using NLog;
using ReactiveUI;

public sealed partial class UpdaterService : ReactiveObject
{
    public static UpdaterService Instance { get; } = new();

    private const string ReleasesUrl = "https://api.github.com/repos/momentum-mod/lumper/releases";

    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // Check for updates as soon as service starts, if haven't checked in last hour.
    private UpdaterService()
    {
        const int updateCheckInterval = 60 * 60;
        long time = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
        long lastCheck = StateService.Instance.LastUpdateCheck;
        StateService.Instance.LastUpdateCheck = time;

        if (time - lastCheck > updateCheckInterval)
            _ = CheckForUpdates();
    }

    /// <summary>
    /// Check whether the running executable is up-to-date with the latest release on Github
    /// <returns>Tuple of whether new release is available and the latest release</returns>
    /// </summary>
    public async Task CheckForUpdates(bool log = false)
    {
        SemVer currentVersion = GetAssemblyVersion();

        if (currentVersion.IsDevBuild)
        {
            _logger.Debug("Running a development build, skipping update check");
            return;
        }

        GithubRelease latestRelease = await FetchGithubUpdates();

        if (GetAssemblyVersion() >= latestRelease.Version)
        {
            const string upToDate = "Current build is up to date";
            if (log)
                _logger.Info(upToDate);
            else
                _logger.Debug(upToDate);
            return;
        }

        if (!await ShowUpdateQuestionDialog(latestRelease))
            return;

        await Update(latestRelease);
    }

    /// <summary>
    /// Get the version embedded in the running Assembly in Semantic Version format (X.Y.Z). This corresponds
    /// to the Git tag used to build the Github release. For dev builds it's always 0.0.0
    /// </summary>
    /// <exception cref="InvalidProgramException"></exception>
    public static SemVer GetAssemblyVersion() =>
        SemVer.Parse(
            Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? throw new InvalidProgramException("Could not determine version of current assembly!")
        );

    /// <summary>
    /// Downloads an update for the program, applies it, and then restarts itself with the new version
    /// </summary>
    private async ValueTask Update(GithubRelease release)
    {
        (OSPlatform os, string osPrefix) = GetPlatform();
        bool isWindows = os == OSPlatform.Windows;
        bool isLinux = !isWindows;

        using var zipStream = new MemoryStream();
        var cts = new CancellationTokenSource();
        var handler = new IoHandler(cts);
        var progressWindow = new IoProgressWindow
        {
            Title = $"Downloading Lumper {release.Version}",
            Handler = handler,
        };

        const int downloadProgressProportion = 80; // 80% of the progress is downloading

        _ = progressWindow.ShowDialog(Program.MainWindow);

        try
        {
            using var httpClient = new HttpClient();
            using HttpResponseMessage response = await httpClient.GetAsync(
                release.GetDownloadUrl(osPrefix),
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token
            );

            if (response.Content.Headers.ContentLength is null or <= 0)
                throw new HttpRequestException("Update Failed: Download is empty / has no content length, aborting.");

            Stream downloadStream = await response.Content.ReadAsStreamAsync(cts.Token);

            byte[] buffer = new byte[64 * 1024];
            int read;
            int length = (int)response.Content.Headers.ContentLength;
            int remaining = length;

            while (
                !handler.Cancelled
                && (
                    read = await downloadStream.ReadAsync(
                        buffer.AsMemory(0, int.Min(buffer.Length, remaining)),
                        cts.Token
                    )
                ) > 0
            )
            {
                float prog = (float)read / length * downloadProgressProportion;
                handler.UpdateProgress(prog, "Downloading release files");
                await zipStream.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                remaining -= read;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to download update!");
            progressWindow.Close();
            return;
        }

        handler.UpdateProgress(0, "Extracting release files...");

        string appDir = AppContext.BaseDirectory;
        string updaterExecutablePath = Path.Combine(appDir, "Lumper.UI" + (os == OSPlatform.Windows ? ".exe" : ""));
        string tmpPath = updaterExecutablePath + ".bak";

        try
        {
            // Zip stream is downloaded into memory, now need to extract it.
            // - On Windows, we can't overwrite the running executable, but we can rename it, then delete after
            //   process exits.
            // - On Linux, we can delete the executable then overwrite it, but on Ubuntu at least, I can't
            //   seem to launch a different executable via Process.Start() (running the original executable
            //   *does* seem to work though??)
            if (os == OSPlatform.Windows)
                File.Move(updaterExecutablePath, tmpPath);
            else if (File.Exists(updaterExecutablePath))
                File.Delete(updaterExecutablePath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to move updater executable!");
            progressWindow.Close();
            return;
        }

        try
        {
            // Using MS ZipArchive because SharpCompress is causing very weird System.Text.Encoding.CodePages errors
            // during ZipArchive.Open calls in release builds.
            ZipFile.ExtractToDirectory(zipStream, AppContext.BaseDirectory, overwriteFiles: true);
        }
        catch (Exception ex)
        {
            File.Move(tmpPath, updaterExecutablePath);
            _logger.Error(ex, "Failed to extract new version, reverting!");
            return;
        }
        finally
        {
            progressWindow.Close();
        }

        if (isWindows)
        {
            _logger.Info("Update completed, restarting...");

            Program.MainWindow.Closed += (_, _) =>
                Process.Start(
                    new ProcessStartInfo("cmd.exe", "/c sleep 1 && del Lumper.UI.exe.bak && Lumper.UI.exe")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WorkingDirectory = appDir,
                    }
                );
        }
        else
        {
            _logger.Info("Update completed!");

            await MessageBoxManager
                .GetMessageBoxStandard("Update complete", "Update is complete, please relaunch the application!")
                .ShowWindowDialogAsync(Program.MainWindow);
        }

        Program.Desktop.Shutdown();
    }

    private async Task<GithubRelease> FetchGithubUpdates()
    {
        using var client = new HttpClient();
        using var request = new HttpRequestMessage { RequestUri = new Uri(ReleasesUrl), Method = HttpMethod.Get };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("Lumper Auto-Updater"); // Github 401s without this!
        HttpResponseMessage response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException("Could not connect to Github API", null, response.StatusCode);

        string jsonString = await response.Content.ReadAsStringAsync();
        List<GithubRelease>? releases = JsonConvert.DeserializeObject<List<GithubRelease>>(jsonString);

        if (releases?[0] is not { } latestRelease)
            throw new InvalidDataException("Failed to parse version JSON");

        return latestRelease;
    }

    private async Task<bool> ShowUpdateQuestionDialog(GithubRelease release)
    {
        if (
            !await MessageBoxManager
                .GetMessageBoxStandard(
                    "Update Available",
                    $"An update is available ({release.Version}), do you want to download it and restart?",
                    ButtonEnum.YesNo
                )
                .ShowWindowDialogAsync(Program.MainWindow)
                .ContinueWith(result => result.Result == ButtonResult.Yes)
        )
            return false;

        if (
            BspService.Instance.IsModified
            && await MessageBoxManager
                .GetMessageBoxStandard(
                    "Unsaved changes",
                    "You have an open BSP with unsaved changes, do you want to save before updating and restarting?",
                    ButtonEnum.YesNo
                )
                .ShowWindowDialogAsync(Program.MainWindow)
                .ContinueWith(result => result.Result == ButtonResult.Yes)
        )
            await BspService.Instance.Save();

        return true;
    }

    private static (OSPlatform, string) GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return (OSPlatform.Windows, "win");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return (OSPlatform.Linux, "linux");

        throw new InvalidProgramException("OS is not supported");
    }

    public record SemVer(int Major, int Minor, int Patch)
    {
        public int Major { get; } = Major;
        public int Minor { get; } = Minor;
        public int Patch { get; } = Patch;

        public static bool operator <(SemVer a, SemVer b) =>
            a.Major < b.Major || a.Minor < b.Minor || a.Patch < b.Patch;

        public static bool operator >(SemVer a, SemVer b) => !(a == b || a < b);

        public static bool operator <=(SemVer a, SemVer b) => !(a > b);

        public static bool operator >=(SemVer a, SemVer b) => !(a < b);

        public bool IsDevBuild => Major == 0 && Minor == 0 && Patch == 0;

        public override string ToString() => $"{Major}.{Minor}.{Patch}";

        public static SemVer Parse(string s)
        {
            // Match pattern of xx.yy.zz
            Match match = SemVerRegex().Match(s);
            if (!match.Success)
                throw new InvalidDataException("Could not parse Major/Minor/Patch version.");

            GroupCollection currentVersion = match.Groups;

            return new SemVer(
                int.Parse(currentVersion[1].ToString(), CultureInfo.InvariantCulture),
                int.Parse(currentVersion[2].ToString(), CultureInfo.InvariantCulture),
                int.Parse(currentVersion[3].ToString(), CultureInfo.InvariantCulture)
            );
        }
    }

    [GeneratedRegex(@"^(\d+)\.(\d+)\.(\d+)")]
    private static partial Regex SemVerRegex();

    // See https://api.github.com/repos/momentum-mod/lumper/releases for the full format
    public record GithubRelease
    {
        [JsonProperty("tag_name")]
        public required string TagName { get; set; }

        [JsonProperty("assets")]
        public required GithubAsset[] Assets { get; set; }

        public SemVer Version => SemVer.Parse(TagName);

        public string GetDownloadUrl(string osPrefix) =>
            Assets.FirstOrDefault(a => a.Name.Contains(osPrefix))?.BrowserDownloadUrl
            ?? throw new InvalidDataException($"Could not find download link for {osPrefix}");
    }

    public record GithubAsset
    {
        [JsonProperty("browser_download_url")]
        public required string BrowserDownloadUrl { get; set; }

        [JsonProperty("name")]
        public required string Name { get; set; }
    }
}
