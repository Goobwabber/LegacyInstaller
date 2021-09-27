using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LegacyInstaller
{
    public partial class Form1 : Form
    {
        private const string VersionsResourcePath = "LegacyInstaller.BSVersions.json";
        private const string LaunchFileResourcePath = "LegacyInstaller.LaunchBS.bat";

        public List<Version> Versions { get; set; } = new List<Version>();
        public Version SelectedVersion { get; private set; }

        public string BSInstallDir { get; private set; }
        public string SelectedVersionInstallDir => BSInstallDir != null && SelectedVersion != null ? Path.Combine($"{BSInstallDir} {SelectedVersion.BSVersion}") : null;

        private SteamProcess _steamProcess = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var bsVersionsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(VersionsResourcePath);
            string versionList = new StreamReader(bsVersionsStream).ReadToEnd();
            Versions = JsonConvert.DeserializeObject<List<Version>>(versionList);

            versionDropdown.Items.AddRange(Versions.ToArray());

            try
            {
                var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var bsRegistryKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980");
                var detectedBSPath = bsRegistryKey.GetValue("InstallLocation");
                if (detectedBSPath != null && Directory.Exists((string)detectedBSPath))
                {
                    BSInstallDir = (string)detectedBSPath;
                    bsPathTextBox.Text = (string)detectedBSPath;
                }
            } catch { }

            try
            {
                var detectedSteamPath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                    "InstallPath",
                    null
                );

                if (detectedSteamPath != null && Directory.Exists((string)detectedSteamPath))
                {
                    steamPathTextBox.Text = (string)detectedSteamPath;
                    _steamProcess = new SteamProcess((string)detectedSteamPath);
                }
            } catch { }

            installButton.Enabled = false;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        private void RefreshUI(bool idle = true)
        {
            versionDropdown.Enabled = idle;
            bsPathTextBox.Enabled = idle;
            bsPathBrowseButton.Enabled = idle;
            steamPathTextBox.Enabled = idle;
            steamPathBrowseButton.Enabled = idle;

            if (!idle)
                installButton.Enabled = false;
            else
            {
                if (_steamProcess != null && BSInstallDir != null)
                {
                    if (SelectedVersionInstallDir != null && Directory.Exists(SelectedVersionInstallDir))
                    {
                        var steamShortcutExists = _steamProcess.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}");
                        installButton.Text = steamShortcutExists ? "Install" : "Add To Steam";
                        installStateLabel.Text = steamShortcutExists ? "Already Installed" : "(Already Installed)";
                        installButton.Enabled = !steamShortcutExists;
                    }
                    else
                    {
                        installButton.Enabled = true;
                        installButton.Text = "Install";
                        installStateLabel.Text = "";
                    }
                }
                else
                    installButton.Enabled = false;
            }
        }

        private async void StealFocus(int delay)
        {
            await Task.Delay(delay);
            this.Invoke((Action)delegate
            {
                this.TopMost = true;
                this.TopMost = false;
                this.Activate();
            });
        }

        private void bsPathBrowseButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            bsPathTextBox.Text = dialog.SelectedPath;
            if (bsPathTextBox.Text != null && Directory.Exists(bsPathTextBox.Text))
                BSInstallDir = bsPathTextBox.Text;
            else
                BSInstallDir = null;

            RefreshUI(true);
        }

        private void bsPathTextBox_TextChanged(object sender, EventArgs e)
        {
            if (bsPathTextBox.Text != null && Directory.Exists(bsPathTextBox.Text))
                BSInstallDir = bsPathTextBox.Text;
            else
                BSInstallDir = null;

            RefreshUI(true);
        }

        private void steamPathBrowseButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() != DialogResult.OK)
                return;

            steamPathTextBox.Text = dialog.SelectedPath;
            if (steamPathTextBox.Text != null && Directory.Exists(steamPathTextBox.Text))
                _steamProcess = new SteamProcess(steamPathTextBox.Text);
            else
                _steamProcess = null;

            RefreshUI(true);
        }

        private void steamPathTextBox_TextChanged(object sender, EventArgs e)
        {
            if (steamPathTextBox.Text != null && Directory.Exists(steamPathTextBox.Text))
                _steamProcess = new SteamProcess(steamPathTextBox.Text);
            else
                _steamProcess = null;

            RefreshUI(true);
        }

        private void versionDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            SelectedVersion = (Version)versionDropdown.SelectedItem;
            RefreshUI(true);
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            if (!_steamProcess.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}") && Directory.Exists(SelectedVersionInstallDir))
            {
                _steamProcess.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));
                installButton.Text = "Install";
                installStateLabel.Text = "Already Installed";
                installButton.Enabled = false;
                return;
            }
            
            installStateLabel.Text = "Downloading...";
            downloadInfoLabel.Text = "Waiting for download to start...";
            RefreshUI(false);

            _ = InstallVersion(SelectedVersion);
        }

        private async Task InstallVersion(Version version)
        {
            // Patch steam if not patched
            if (!_steamProcess.IsPatched)
                await _steamProcess.PatchDownloadDepot();

            // Start download
            StealFocus(200);
            await _steamProcess.DownloadDepot(version.ManifestId, FileSystemChanged);

            // Copy files
            this.Invoke((Action)delegate { installStateLabel.Text = "Copying..."; });
            Directory.CreateDirectory(SelectedVersionInstallDir);
            var watcher = new FileSystemWatcher(SelectedVersionInstallDir);
            watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true; 
            watcher.Changed += FileSystemChanged;
            await CopyDirectory(_steamProcess.ContentAppDepotDir, SelectedVersionInstallDir);

            // Install to steam
            this.Invoke((Action)delegate { installStateLabel.Text = "Installing..."; });
            await CopyLaunchFileTo(SelectedVersionInstallDir);
            _steamProcess.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));

            // Restart steam
            this.Invoke((Action)delegate { installStateLabel.Text = "Already Installed"; });
            await _steamProcess.Restart();

            // Enable UI
            this.Invoke((Action)delegate { RefreshUI(true); });
        }

        private void FileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(_steamProcess.ContentAppDepotDir) && !e.FullPath.Contains(SelectedVersionInstallDir))
                return;

            this.Invoke((Action)delegate
            {
                downloadInfoLabel.Text = DateTime.Now.ToString("ffffff") + ": " + e.FullPath.Replace(_steamProcess.ContentAppDepotDir, "").Replace(SelectedVersionInstallDir, "");
            });
        }

        private async Task CopyDirectory(string sourceDir, string targetDir)
        {
            try
            {
                Directory.CreateDirectory(targetDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                {
                    var fileSource = File.OpenRead(file);
                    var fileTarget = File.Create(Path.Combine(targetDir, Path.GetFileName(file)));
                    await fileSource.CopyToAsync(fileTarget);
                }

                foreach (var directory in Directory.GetDirectories(sourceDir))
                    await CopyDirectory(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
            }
            catch { }
        }

        private async Task CopyLaunchFileTo(string targetDir)
        {
            var filePath = Path.Combine(targetDir, "LaunchBS.bat");
            var fileResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LaunchFileResourcePath);
            var fileStream = File.Create(filePath);
            fileResourceStream.Seek(0, SeekOrigin.Begin);
            await fileResourceStream.CopyToAsync(fileStream);
            fileStream.Close();
        }
    }
}
