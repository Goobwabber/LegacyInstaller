using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace LegacyInstaller
{
    public partial class MainWindow : Window
    {
        public static Label downloadInfoLabelObject;
        private const string VersionsResourcePath = "LegacyInstaller.BSVersions.json";
        private const string LaunchFileResourcePath = "LegacyInstaller.LaunchBS.bat";

        public List<Version> Versions { get; set; } = new List<Version>();
        public Version SelectedVersion { get; private set; }

        public string BSInstallDir { get; private set; }
        public string SelectedVersionInstallDir => BSInstallDir != null && SelectedVersion != null ? $"{BSInstallDir} {SelectedVersion.BSVersion}" : null;

        private SteamProcess _steamProcess = null;
        public MainWindow()
        {
            InitializeComponent();
            var bsVersionsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(VersionsResourcePath);
            string versionList = new StreamReader(bsVersionsStream).ReadToEnd();
            Versions = JsonConvert.DeserializeObject<List<Version>>(versionList);
            AddToCombo(Versions.ToArray(), versionDropdown);

            var detectedBSPath = Utilities.DetectBeatSaberInstallPath();
            if (detectedBSPath != null)
            {
                BSInstallDir = (string)detectedBSPath;
                bsPathTextBox.Text = (string)detectedBSPath;
            }

            var detectedSteamPath = Utilities.DetectSteamInstallPath();
            if (detectedSteamPath != null)
            {
                steamPathTextBox.Text = (string)detectedSteamPath;
                _steamProcess = new SteamProcess((string)detectedSteamPath);
            }

            RefreshUI(true);
            Task.Run(RefreshInternal);
        }

        public void AddToCombo(Array array, ComboBox c)
        {
            foreach (var a in array)
            {
                c.Items.Add(a);
            }
        }
        private void RefreshUI(bool idle = true)
        {
            versionDropdown.IsEnabled = idle;
            bsPathTextBox.IsEnabled = idle;
            bsPathBrowseButton.IsEnabled = idle;
            steamPathTextBox.IsEnabled = idle;
            steamPathBrowseButton.IsEnabled = idle;

            if (!idle)
                installButton.IsEnabled = false;
            else
            {
                if (_steamProcess != null && BSInstallDir != null && _steamProcess.CurrentUserId != 0 && _steamProcess.Process != null)
                {
                    installButton.IsEnabled = true;

                    if (SelectedVersionInstallDir != null && Directory.Exists(SelectedVersionInstallDir))
                    {
                        var steamShortcutExists = _steamProcess.Shortcuts.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}");
                        installButton.Content = steamShortcutExists ? "Uninstall" : "Add To Steam";
                        installStateLabel.Content = steamShortcutExists ? "Already Installed" : "(Already Installed)";

                    }
                    else
                    {
                        installButton.Content = "Install";
                        installStateLabel.Content = "";
                    }
                }
                else
                {
                    installStateLabel.Content = "";
                    installButton.IsEnabled = false;
                }

                var labelText = "";
                if (_steamProcess == null)
                    labelText += "Please set your Steam install directory.\n";
                if (BSInstallDir == null)
                    labelText += "Please set your Beat Saber install directory.\n";
                if (_steamProcess != null && (_steamProcess.CurrentUserId == 0 || _steamProcess.Process == null))
                    labelText += "Please log into Steam.\n";
                downloadInfoLabelObject.Content = labelText;
            }
        }
        private async Task RefreshInternal()
        {
            if (_steamProcess == null || BSInstallDir == null)
                return;

            await _steamProcess.WaitForMainWindow();
            this.Dispatcher.Invoke((Action)delegate { RefreshUI(); });

            var currentLaunchBSChecksum = Utilities.GenerateStringChecksum(await new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(LaunchFileResourcePath)).ReadToEndAsync());
            foreach (var version in Versions)
            {
                var versionExists = Directory.Exists($"{BSInstallDir} {version.BSVersion}");
                var shortcutExists = _steamProcess.Shortcuts.CheckForSteamShortcut($"Beat Saber {version.BSVersion}");

                if (versionExists && !shortcutExists)
                    _steamProcess.Shortcuts.AddSteamShortcut(new SteamShortcut($"Beat Saber {version.BSVersion}", $"{BSInstallDir} {version.BSVersion}", "LaunchBS.bat"));
                if (!versionExists && shortcutExists)
                    _steamProcess.Shortcuts.DeleteSteamShortcut($"Beat Saber {version.BSVersion}");
                if (!versionExists)
                    continue;

                // Restore LaunchBS.bat if it doesnt exist or is old
                var launchBSPath = Path.Combine($"{BSInstallDir} {version.BSVersion}", "LaunchBS.bat");
                if (!File.Exists(launchBSPath) || currentLaunchBSChecksum != Utilities.GenerateStringChecksum(File.ReadAllText(launchBSPath)))
                    await CopyLaunchFileTo($"{BSInstallDir} {version.BSVersion}");
            }

            if (_steamProcess.Shortcuts.HasChanged)
                await RestartSteam();
        }
        private async void StealFocus(int delay)
        {
            await Task.Delay(delay);
            await Task.Run(() => this.Dispatcher.Invoke((Action)delegate
            {
                this.Topmost = true;
                this.Topmost = false;
                this.Activate();
            }));
        }

        private async Task RestartSteam(string openTo = null)
        {
            this.Dispatcher.Invoke((Action)delegate
            {
                installStateLabel.Content = "Restarting Steam...";
                downloadInfoLabel.Content = "Waiting for Steam login...";
                RefreshUI(false);
            });
            await _steamProcess.Restart(openTo);
            this.Dispatcher.Invoke((Action)delegate
            {
                installStateLabel.Content = "Done!";
                downloadInfoLabel.Content = "";
                RefreshUI(true);
            });
        }


        private void bsPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            bsPathTextBox.Text = dialog.SelectedPath;
            if (bsPathTextBox.Text != null && Directory.Exists(bsPathTextBox.Text))
                BSInstallDir = bsPathTextBox.Text;
            else
                BSInstallDir = null;
        }

        private void bsPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (bsPathTextBox.Text != null && Directory.Exists(bsPathTextBox.Text))
            {
                BSInstallDir = bsPathTextBox.Text;
                RefreshUI();
            }
            else
                BSInstallDir = null;
        }

        private void steamPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            steamPathTextBox.Text = dialog.SelectedPath;
            if (steamPathTextBox.Text != null && Directory.Exists(steamPathTextBox.Text))
                _steamProcess = new SteamProcess(steamPathTextBox.Text);
            else
                _steamProcess = null;
        }

        private void steamPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (steamPathTextBox.Text != null && Directory.Exists(steamPathTextBox.Text))
            {
                _steamProcess = new SteamProcess(steamPathTextBox.Text);
                RefreshUI();
            }
            else
                _steamProcess = null;
        }



        private void versionDropdown_SelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedVersion = (Version)versionDropdown.SelectedItem;
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(SelectedVersionInstallDir))
            {
                if (!_steamProcess.Shortcuts.CheckForSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}"))
                    _steamProcess.Shortcuts.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));
                else
                {
                    _steamProcess.Shortcuts.DeleteSteamShortcut($"Beat Saber {SelectedVersion.BSVersion}");
                    Directory.Delete(SelectedVersionInstallDir, true);
                }

                _ = RestartSteam();
                return;
            }

            installStateLabel.Content = "Downloading...";
            downloadInfoLabel.Content = "Waiting for download to start...";

            _ = InstallVersion(SelectedVersion);
        }

        private async Task InstallVersion(Version version)
        {
            // Start download
            StealFocus(500); // Steal focus after download start
            await _steamProcess.Downloader.DownloadDepot(version.ManifestId, FileSystemChanged);

            // Copy files
            this.Dispatcher.Invoke((Action)delegate { installStateLabel.Content = "Copying..."; });
            Directory.CreateDirectory(SelectedVersionInstallDir);
            var watcher = new FileSystemWatcher(SelectedVersionInstallDir);
            watcher.NotifyFilter = NotifyFilters.Attributes | NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            watcher.Changed += FileSystemChanged;
            await Utilities.CopyDirectory(_steamProcess.Downloader.ContentAppDepotDir, SelectedVersionInstallDir);

            // Install to steam
            this.Dispatcher.Invoke((Action)delegate { installStateLabel.Content = "Installing..."; });
            await CopyLaunchFileTo(SelectedVersionInstallDir);
            _steamProcess.Shortcuts.AddSteamShortcut(new SteamShortcut($"Beat Saber {SelectedVersion.BSVersion}", SelectedVersionInstallDir, "LaunchBS.bat"));

            // Restart steam
            var steamAppId = Utilities.GetSteamAppId($"Beat Saber {SelectedVersion.BSVersion}", $"{BSInstallDir} {SelectedVersion.BSVersion}", "LaunchBS.bat");
            await RestartSteam($"steam://nav/games/details/{steamAppId}");

            // Enable UI
            this.Dispatcher.Invoke((Action)delegate { RefreshUI(true); });
        }

        private void FileSystemChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.FullPath.Contains(_steamProcess.Downloader.ContentAppDepotDir) && !e.FullPath.Contains(SelectedVersionInstallDir))
                return;

            this.Dispatcher.Invoke((Action)delegate
            {
                downloadInfoLabel.Content = DateTime.Now.ToString("ffffff") + ": " + e.FullPath.Replace(_steamProcess.Downloader.ContentAppDepotDir, "").Replace(SelectedVersionInstallDir, "");
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

        private void downloadInfoLabel_Initialized(object sender, EventArgs e)
        {
            downloadInfoLabelObject = (Label)sender;
            RefreshUI();
        }
    }
}
