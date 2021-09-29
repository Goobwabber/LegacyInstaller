using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LegacyInstaller
{
    class SteamProcess
    {
        private Process _process = null;
        public Process Process 
        { 
            get
            {
                if (_process == null || _process.HasExited)
                    _process = Process.GetProcessesByName("steam").FirstOrDefault();
                return _process;
            }
        }

        private SteamShortcuts _shortcuts = null;
        public SteamShortcuts Shortcuts
        {
            get
            {
                if (_shortcuts == null || _shortcuts.UserId != CurrentUserId || _shortcuts.ProcessId != Process.Id)
                    _shortcuts = new SteamShortcuts(InstallDir, CurrentUserId, Process.Id);
                return _shortcuts;
            }
        }

        public readonly SteamDownloader Downloader;

        public int CurrentUserId => Utilities.GetCurrentSteamUser();
        public bool HasExited => Process == null || Process.HasExited;

        public readonly string InstallDir;
        public readonly string SteamExecutable;

        private readonly string[] MainWindowTitles = new[] { "Steam", "Servers", "Friends", "Default IME" };

        public SteamProcess(string installDir)
        {
            InstallDir = installDir;
            SteamExecutable = Path.Combine(InstallDir, "steam.exe");
            Downloader = new SteamDownloader(InstallDir);
        }

        public async Task Restart()
        {
            await Task.Run(() =>
            {
                Process.Kill();
                Process.WaitForExit();
                var steamProcess = Process.Start(SteamExecutable, "-silent +open steam://open/library");
                steamProcess.WaitForInputIdle();
            });
            await WaitForMainWindow();
        }

        private const int WM_GETTEXT = 0x000D;

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        public async Task WaitForMainWindow() => await Task.Run(() =>
        {
            List<string> windowTitles = new List<string>();
            while (!MainWindowTitles.All(title => windowTitles.Contains(title)))
            {
                Process.Refresh();
                var handles = new List<IntPtr>();
                foreach (ProcessThread thread in Process.Threads)
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => { handles.Add(hWnd); return true; }, IntPtr.Zero);
                foreach (var handle in handles)
                {
                    StringBuilder message = new StringBuilder(1000);
                    SendMessage(handle, WM_GETTEXT, message.Capacity, message);
                    if (!string.IsNullOrEmpty(message.ToString()))
                        windowTitles.Add(message.ToString());
                }
            }
        });
    }
}
