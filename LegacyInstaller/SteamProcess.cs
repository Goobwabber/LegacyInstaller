using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LegacyInstaller
{
    class SteamProcess
    {
        public Process Process => Process.GetProcessesByName("steam").FirstOrDefault();
        public int CurrentUserId => (int)RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
            .OpenSubKey(@"SOFTWARE\Valve\Steam\ActiveProcess").GetValue("ActiveUser");
        public bool HasExited => Process == null || Process.HasExited;
        public bool IsPatched => Process != null && Process.Id == _patchedProcessId;
        public bool ShortcutsChanged => _shortcutsChangedProcessId != 0 && Process != null && Process.Id == _shortcutsChangedProcessId;

        public string InstallDir { get; private set; }
        public string ContentDir => Path.Combine(InstallDir, "steamapps", "content");
        public string ContentAppDepotDir => Path.Combine(ContentDir, $"app_{SteamAppId}", $"depot_{SteamDepotId}");
        public string ShortcutsFile => Path.Combine(InstallDir, "userdata", CurrentUserId.ToString(), "config", "shortcuts.vdf");

        private const string SteamAppId = "620980";
        private const string SteamDepotId = "620981";

        private int _patchedProcessId = 0;
        private int _shortcutsChangedProcessId = 0;
        private FileSystemWatcher _contentDirWatcher;

        public SteamProcess(string installDir)
        {
            InstallDir = installDir;
            Directory.CreateDirectory(ContentDir);
            _contentDirWatcher = new FileSystemWatcher(ContentDir);
            _contentDirWatcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            _contentDirWatcher.IncludeSubdirectories = true;
            _contentDirWatcher.EnableRaisingEvents = true;
            _contentDirWatcher.Changed += ContentDirChanged;
        }

        public async Task Restart()
        {
            Process.Kill();
            await Task.Delay(5000);
            Process.Start(Path.Combine(InstallDir, "steam.exe"));
        }

        public async Task PatchDownloadDepot()
        {
            await Task.Run(() =>
            {
                try { SteamPatcher.ApplyPatch(); }
                catch (SteamPatcher.PatchAlreadyAppliedException) { }
            });
            _patchedProcessId = Process.Id;
        }

        private TaskCompletionSource<bool> _downloadDepotTcs;

        public async Task DownloadDepot(string manifestId, FileSystemEventHandler eventHandler = null)
        {
            if (_downloadDepotTcs != null)
                throw new DownloadInProgressException();

            if (eventHandler != null)
                _contentDirWatcher.Changed += eventHandler;

            _downloadDepotTcs = new TaskCompletionSource<bool>();

            var path = Path.Combine(InstallDir, "steam");
            var args = $"-console +download_depot 620980 620981 {manifestId}";
            Process.Start(path, args);

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

        public bool CheckForSteamShortcut(string shortcut)
        {
            if (CurrentUserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsFile))
                return false;
            var shortcuts = File.ReadAllText(ShortcutsFile);
            return shortcuts.Contains($"\x00{shortcut}\x00");
        }

        public void AddSteamShortcut(SteamShortcut shortcut)
        {
            if (CurrentUserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsFile))
                File.WriteAllText(ShortcutsFile, "\x00shortcuts\x00\x08\x08");

            if (File.ReadAllText(ShortcutsFile).Contains(shortcut.AppName))
                return;

            var shortcuts = File.ReadAllBytes(ShortcutsFile);

            var newShortcuts = shortcuts.Take(shortcuts.Length - 2); // remove file end
            newShortcuts = newShortcuts.Concat(shortcut.GetBytes(GetSteamShortcutCount())); // append new shortcut
            newShortcuts = newShortcuts.Append((byte)0x08).Append((byte)0x08); // add file end

            File.WriteAllBytes(ShortcutsFile, newShortcuts.ToArray());

            if (!HasExited)
                _shortcutsChangedProcessId = Process.Id;
        }

        public void DeleteSteamShortcut(string appName)
        {
            if (CurrentUserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsFile))
                return;

            var shortcuts = File.ReadAllText(ShortcutsFile, Encoding.ASCII);
            if (!shortcuts.Contains(appName))
                return;

            var newShortcuts = Regex.Replace(shortcuts, @"\x00[\d]\x00(?!.*?\x00[\d]\x00.*?\x01AppName\x00" + appName + @"\x00).*?\x08\x08", "");
            File.WriteAllText(ShortcutsFile, newShortcuts, Encoding.ASCII);

            if (!HasExited)
                _shortcutsChangedProcessId = Process.Id;
        }

        public int GetSteamShortcutCount()
        {
            var shortcuts = File.ReadAllText(ShortcutsFile);

            int highestIndex = -1;
            var matches = Regex.Matches(shortcuts, @"\x00([\d])\x00");
            foreach (Match match in matches)
            {
                int index = Int32.Parse(match.Groups[1].Value);
                if (highestIndex < index)
                    highestIndex = index;
            }

            return highestIndex + 1;
        }

        public class DownloadInProgressException : Exception
        {
            public DownloadInProgressException() : base() { }
            public DownloadInProgressException(string message) : base(message) { }
            public DownloadInProgressException(string message, Exception inner) : base(message, inner) { }
        }

        public class NotLoggedInException : Exception
        {
            public NotLoggedInException() : base() { }
            public NotLoggedInException(string message) : base(message) { }
            public NotLoggedInException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
