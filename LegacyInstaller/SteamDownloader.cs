using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace LegacyInstaller
{
    class SteamDownloader
    {
        public readonly string SteamExecutable;
        public readonly string ContentDir;
        public readonly string ContentAppDepotDir;

        private const string SteamAppId = "620980";
        private const string SteamDepotId = "620981";

        private FileSystemWatcher _contentDirWatcher;

        public SteamDownloader(string installDir)
        {
            SteamExecutable = Path.Combine(installDir, "steam.exe");
            ContentDir = Path.Combine(installDir, "steamapps", "content");
            ContentAppDepotDir = Path.Combine(ContentDir, $"app_{SteamAppId}", $"depot_{SteamDepotId}");
            Directory.CreateDirectory(ContentDir);
            _contentDirWatcher = new FileSystemWatcher(ContentDir);
            _contentDirWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            _contentDirWatcher.IncludeSubdirectories = true;
            _contentDirWatcher.EnableRaisingEvents = true;
            _contentDirWatcher.Changed += ContentDirChanged;
        }

        private async Task PatchDownloadDepot() => await Task.Run(() =>
        {
            try { SteamPatcher.ApplyPatch(); }
            catch (SteamPatcher.PatchAlreadyAppliedException) { }
            catch (SteamPatcher.StringNotFoundException) { }
        });

        private TaskCompletionSource<bool> _downloadDepotTcs;

        public async Task DownloadDepot(string manifestId, FileSystemEventHandler eventHandler = null)
        {
            await PatchDownloadDepot();

            if (_downloadDepotTcs != null)
                throw new DownloadInProgressException();

            if (eventHandler != null)
                _contentDirWatcher.Changed += eventHandler;

            _downloadDepotTcs = new TaskCompletionSource<bool>();

            Process.Start(SteamExecutable, $"-console +download_depot {SteamAppId} {SteamDepotId} {manifestId}");

            await _downloadDepotTcs.Task;
            await Task.Delay(5000);

            _contentDirWatcher.Changed -= eventHandler;
        }

        private void ContentDirChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(ContentAppDepotDir))
                return;

            if (e.FullPath.Contains("UnityPlayer.dll") && e.ChangeType == WatcherChangeTypes.Changed && _downloadDepotTcs != null)
                _downloadDepotTcs.SetResult(true);
        }

        public class DownloadInProgressException : Exception
        {
            public DownloadInProgressException() : base() { }
            public DownloadInProgressException(string message) : base(message) { }
            public DownloadInProgressException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
