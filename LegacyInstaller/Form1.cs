using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
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
        public string SelectedVersionInstallDir => BSInstallDir != null && SelectedVersion != null ? $"{BSInstallDir} {SelectedVersion.BSVersion}" : null;

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

            _ = RefreshInternal();
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
                    installButton.Enabled = true;

                    if (SelectedVersionInstallDir != null && Directory.Exists(SelectedVersionInstallDir))
                    {
                        var steamShortcutExists = _steamProcess.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}");
                        installButton.Text = steamShortcutExists ? "Uninstall" : "Add To Steam";
                        installStateLabel.Text = steamShortcutExists ? "Already Installed" : "(Already Installed)";

                    }
                    else
                    {
                        installButton.Text = "Install";
                        installStateLabel.Text = "";
                    }
                }
                else
                    installButton.Enabled = false;
            }
        }

        private bool _isRefreshing;
        private async Task RefreshInternal()
        {
            if (_isRefreshing == true)
                return;

            _isRefreshing = true;

            var labelText = "";
            if (_steamProcess == null)
                labelText += "Please set your Steam install directory.";
            if (BSInstallDir == null)
                labelText += "Please set your Beat Saber install directory.";
            if (_steamProcess != null && _steamProcess.CurrentUserId == 0)
                labelText += "Please log into Steam.";
            this.Invoke((Action)delegate { downloadInfoLabel.Text = labelText; });

            if (BSInstallDir == null || _steamProcess == null || _steamProcess.HasExited)
                return;

            if (_steamProcess.CurrentUserId == 0)
                this.Invoke((Action)delegate { RefreshUI(false); });

            //await _steamProcess.WaitForLogin();
            //this.Invoke((Action)delegate { RefreshUI(true); });

            var currentLaunchBSChecksum = Utilities.GenerateStringChecksum(await new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(LaunchFileResourcePath)).ReadToEndAsync());
            foreach (var version in Versions)
            {
                var versionExists = Directory.Exists($"{BSInstallDir} {version.BSVersion}");
                var shortcutExists = _steamProcess.CheckForSteamShortcut($"Beat Saber {version.BSVersion}");

                if (versionExists && !shortcutExists)
                    _steamProcess.AddSteamShortcut(new SteamShortcut($"Beat Saber {version.BSVersion}", $"{BSInstallDir} {version.BSVersion}", "LaunchBS.bat"));
                if (!versionExists && shortcutExists)
                    _steamProcess.DeleteSteamShortcut($"Beat Saber {version.BSVersion}");
                if (!versionExists)
                    continue;

                // Restore LaunchBS.bat if it doesnt exist or is old
                var launchBSPath = Path.Combine($"{BSInstallDir} {version.BSVersion}", "LaunchBS.bat");
                if (!File.Exists(launchBSPath) || currentLaunchBSChecksum != Utilities.GenerateStringChecksum(File.ReadAllText(launchBSPath)))
                    await CopyLaunchFileTo($"{BSInstallDir} {version.BSVersion}");
            }

            if (_steamProcess.ShortcutsChanged)
            {
                this.Invoke((Action)delegate
                {
                    installStateLabel.Text = "Restarting Steam...";
                    downloadInfoLabel.Text = "Waiting for Steam login...";
                    RefreshUI(false);
                });
                await _steamProcess.Restart();
                this.Invoke((Action)delegate
                {
                    installStateLabel.Text = "Done!";
                    downloadInfoLabel.Text = "";
                    RefreshUI(true);
                });
            }

            _isRefreshing = false;
        }

        private async void StealFocus(int delay)
        {
            await Task.Delay(delay);
            await Task.Run(() => this.Invoke((Action)delegate
            {
                this.TopMost = true;
                this.TopMost = false;
                this.Activate();
            }));
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

        private void versionDropdown_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;

            string text = ((ComboBox)sender).Items[e.Index].ToString();         

            Brush brush = Brushes.White;
            if (!e.State.HasFlag(DrawItemState.ComboBoxEdit) && Directory.Exists($"{BSInstallDir} {Versions[e.Index]}"))
                brush = Brushes.LightGreen;

            if (e.State.HasFlag(DrawItemState.Selected) && !e.State.HasFlag(DrawItemState.ComboBoxEdit))
                brush = Brushes.LightBlue;

            e.Graphics.FillRectangle(brush, e.Bounds);
            e.Graphics.DrawString(text, ((Control)sender).Font, Brushes.Black, e.Bounds.X, e.Bounds.Y);
        }

        private void installButton_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(SelectedVersionInstallDir))
            {
                if (!_steamProcess.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}"))
                    _steamProcess.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));
                else
                {
                    _steamProcess.DeleteSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}");
                    Directory.Delete(SelectedVersionInstallDir, true);
                }

                RefreshUI(true);
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
            await Utilities.CopyDirectory(_steamProcess.ContentAppDepotDir, SelectedVersionInstallDir);

            // Install to steam
            this.Invoke((Action)delegate { installStateLabel.Text = "Installing..."; });
            await CopyLaunchFileTo(SelectedVersionInstallDir);
            _steamProcess.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));

            // Restart steam
            this.Invoke((Action)delegate
            {
                installStateLabel.Text = "Restarting Steam...";
                downloadInfoLabel.Text = "Waiting for Steam login...";
            });
            await _steamProcess.Restart();
            this.Invoke((Action)delegate 
            { 
                installStateLabel.Text = "Already Installed";
                downloadInfoLabel.Text = "";
            });

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
