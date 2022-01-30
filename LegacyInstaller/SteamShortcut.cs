using System.Text;

namespace LegacyInstaller
{
    class SteamShortcut
    {
        public string AppName { get; set; }
        public string Path { get; set; }
        public string Executable { get; set; }
        public bool IsHidden { get; set; } = false;
        public bool AllowDesktopConfig { get; set; } = true;
        public bool AllowOverlay { get; set; } = true;
        public bool OpenVR { get; set; } = true;

        public SteamShortcut(string appName, string path, string executable)
        {
            AppName = appName;
            Path = path;
            Executable = executable;
        }

        public byte[] GetBytes(int shortcutIndex)
        {
            string strShortcut =
                "\x00" + shortcutIndex          + "\x00" +
                "\x02" + "appid"                + "\x00" + "\x00\x00\x00\x00"                       + "" +
                "\x01" + "AppName"              + "\x00" + AppName                                  + "\x00" +
                "\x01" + "Exe"                  + "\x00" + $"\"{Path}\\{Executable}\""              + "\x00" +
                "\x01" + "StartDir"             + "\x00" + $"\"{Path}\\\""                          + "\x00" +
                "\x01" + "icon"                 + "\x00" + $"\"{Path}\\Beat Saber.exe\""            + "\x00" +
                "\x01" + "ShortcutPath"         + "\x00" + ""                                       + "\x00" +
                "\x01" + "LaunchOptions"        + "\x00" + ""                                       + "\x00" +
                "\x02" + "IsHidden"             + "\x00" + (IsHidden ? "\x01" : "\x00")             + "\x00\x00\x00" +
                "\x02" + "AllowDesktopConfig"   + "\x00" + (AllowDesktopConfig ? "\x01" : "\x00")   + "\x00\x00\x00" +
                "\x02" + "AllowOverlay"         + "\x00" + (AllowOverlay ? "\x01" : "\x00")         + "\x00\x00\x00" +
                "\x02" + "OpenVR"               + "\x00" + (OpenVR ? "\x01" : "\x00")               + "\x00\x00\x00" +
                "\x02" + "Devkit"               + "\x00" + "\x00"                                   + "\x00\x00\x00" +
                "\x01" + "DevkitGameID"         + "\x00" + ""                                       + "\x00" +
                "\x02" + "DevkitOverrideAppID"  + "\x00" + "\x00\x00\x00"                           + "\x00" +
                "\x02" + "LastPlayTime"         + "\x00" + "\x00\x00\x00\x00"                       + "" +
                "\x00" + "tags"                 + "\x00" + ""                                       + "\x08\x08";

            return Encoding.UTF8.GetBytes(strShortcut);
        }
    }
}
