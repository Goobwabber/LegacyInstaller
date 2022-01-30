using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LegacyInstaller
{
    class SteamShortcuts
    {
        public readonly string ShortcutsPath;
        public readonly int UserId;
        public readonly int ProcessId;

        public bool HasChanged { get; private set; }

        public SteamShortcuts(string installDir, int userId, int processId)
        {
            ShortcutsPath = Path.Combine(installDir, "userdata", userId.ToString(), "config", "shortcuts.vdf");
            UserId = userId;
            ProcessId = processId;
        }

        public bool CheckForSteamShortcut(string shortcut)
        {
            if (UserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsPath))
                return false;
            var shortcuts = File.ReadAllText(ShortcutsPath);
            return shortcuts.Contains($"\x00{shortcut}\x00");
        }

        public void AddSteamShortcut(SteamShortcut shortcut)
        {
            if (UserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsPath))
                File.WriteAllText(ShortcutsPath, "\x00shortcuts\x00\x08\x08");

            if (File.ReadAllText(ShortcutsPath).Contains($"\x00{shortcut.AppName}\x00"))
                return;

            var shortcuts = File.ReadAllBytes(ShortcutsPath);

            var newShortcuts = shortcuts.Take(shortcuts.Length - 2); // remove file end
            newShortcuts = newShortcuts.Concat(shortcut.GetBytes(GetSteamShortcutCount())); // append new shortcut
            newShortcuts = newShortcuts.Append((byte)0x08).Append((byte)0x08); // add file end

            File.WriteAllBytes(ShortcutsPath, newShortcuts.ToArray());

            HasChanged = true;
        }

        public void DeleteSteamShortcut(string appName)
        {
            if (UserId == 0)
                throw new NotLoggedInException();

            if (!File.Exists(ShortcutsPath))
                return;

            var shortcuts = File.ReadAllText(ShortcutsPath, Encoding.ASCII);
            if (!shortcuts.Contains($"\x00{appName}\x00"))
                return;

            var newShortcuts = Regex.Replace(shortcuts, @"\x00[\d]\x00(?!.*?\x00[\d]\x00.*?\x01AppName\x00" + appName + @"\x00).*?\x08\x08", "");
            File.WriteAllText(ShortcutsPath, newShortcuts, Encoding.ASCII);

            HasChanged = true;
        }

        public int GetSteamShortcutCount()
        {
            var shortcuts = File.ReadAllText(ShortcutsPath);

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

        public class NotLoggedInException : Exception
        {
            public NotLoggedInException() : base() { }
            public NotLoggedInException(string message) : base(message) { }
            public NotLoggedInException(string message, Exception inner) : base(message, inner) { }
        }
    }
}
