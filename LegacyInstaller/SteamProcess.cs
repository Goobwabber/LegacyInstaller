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

        public SteamProcess(string installDir)
        {
            InstallDir = installDir;
            SteamExecutable = Path.Combine(InstallDir, "steam.exe");
            Downloader = new SteamDownloader(InstallDir);
        }

        public async Task Restart(string openTo = null)
        {
            await Task.Run(() =>
            {
                Process.Kill();
                Process.WaitForExit();
                var args = $"-silent +open steam://open/library" + (openTo != null ? $" +open {openTo}" : "");
                Debug.WriteLine(args);
                var steamProcess = new Process();
                steamProcess.StartInfo.FileName = SteamExecutable;
                steamProcess.StartInfo.Arguments = args;
                steamProcess.StartInfo.UseShellExecute = true;
                steamProcess.Start();
                steamProcess.WaitForInputIdle();
            });
            await WaitForMainWindow();
        }



        private const int MainWindowPollTime = 500;

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);
        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, StringBuilder lParam);

        public async Task WaitForMainWindow()
        {
            // when Steam restarts, the user id still remains
            while (Utilities.GetCurrentSteamUser() != 0)
            {
                if (Process == null)
                    continue;

                await Task.Delay(MainWindowPollTime);
            }
            // when Steam logs in, the user id is reset to 0
            while (Utilities.GetCurrentSteamUser() == 0)
            {
                if (Process == null)
                    continue;

                await Task.Delay(MainWindowPollTime);
            }
            // when login is successful and window is loaded, the user id is set
        }
    }
}
