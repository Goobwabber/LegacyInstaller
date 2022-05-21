﻿using Microsoft.Win32;
using System;
using System.Data.HashFunction.CRC;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LegacyInstaller
{
    public static class Utilities
    {
        public static async Task CopyDirectory(string sourceDir, string targetDir)
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

        public static int Search(byte[] src, byte[] pattern)
        {
            int maxFirstCharSlot = src.Length - pattern.Length + 1;
            for (int i = 0; i < maxFirstCharSlot; i++)
            {
                if (src[i] != pattern[0]) // compare only first byte
                    continue;

                // found a match on first byte, now try to match rest of the pattern
                for (int j = pattern.Length - 1; j >= 1; j--)
                {
                    if (src[i + j] != pattern[j]) break;
                    if (j == 1) return i;
                }
            }
            return -1;
        }

        public static string GenerateChecksum(string targetDir)
        {
            var sha = SHA1.Create();

            var files = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories);
            var bytes = new byte[0];
            foreach (var file in files)
            {
                if (file.Contains("LaunchBS"))
                    continue;
                if (!File.Exists(file))
                    continue;
                bytes = bytes.Concat(sha.ComputeHash(File.ReadAllBytes(file))).ToArray();
            }
            return BitConverter.ToString(sha.ComputeHash(bytes));
        }

        public static string GenerateStringChecksum(string input)
        {
            var sha = SHA1.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        public static string DetectSteamInstallPath()
        {
            try
            {
                var detectedSteamPath = Registry.GetValue(
                    @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam",
                    "InstallPath",
                    null
                );

                if (detectedSteamPath != null && Directory.Exists((string)detectedSteamPath))
                    return (string)detectedSteamPath;
            }
            catch { }
            return null;
        }

        public static string DetectBeatSaberInstallPath()
        {
            try
            {
                var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                var bsRegistryKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 620980");
                if (bsRegistryKey != null)
                {
                    var bsInstallPath = bsRegistryKey.GetValue("InstallLocation");
                    if (bsInstallPath != null && Directory.Exists((string)bsInstallPath))
                        return (string)bsInstallPath;
                }
                if (Directory.Exists("C:/Program Files (x86)/Steam/steamapps/common/Beat Saber"))
                {
                    return "C:/Program Files (x86)/Steam/steamapps/common/Beat Saber";
                }
            }
            catch { }
            return "Please enter the Beat Saber install path manually";
        }

        public static int GetCurrentSteamUser()
            => (int)RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Valve\Steam\ActiveProcess").GetValue("ActiveUser");

        public static UInt64 GetSteamAppId(string appName, string path, string exe)
        {
            var crc = CRCFactory.Instance.Create(new CRCConfig
            {
                HashSizeInBits = 32,
                Polynomial = 0x04C11DB7,
                ReflectIn = true,
                InitialValue = 0xffffffff,
                ReflectOut = true,
                XOrOut = 0xffffffff
            });

            byte[] inputBytes = Encoding.UTF8.GetBytes("\"" + Path.Combine(path, exe) + "\"" + appName);
            UInt64 top32 = BitConverter.ToUInt32(crc.ComputeHash(inputBytes).Hash, 0) | 0x80000000;
            UInt64 gameId = (top32 << 32) | 0x02000000;

            return gameId;
        }
    }
}
