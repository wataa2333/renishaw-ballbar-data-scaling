using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Wataa.QC20Tool
{
    internal static class AppPaths
    {
        private static string dataRoot;

        public static string DataRoot
        {
            get
            {
                if (dataRoot == null)
                    dataRoot = SelectWritableDataRoot();
                return dataRoot;
            }
        }

        public static string Logs
        {
            get { return EnsureDirectory(Path.Combine(DataRoot, "Logs")); }
        }

        public static string Backups
        {
            get { return EnsureDirectory(Path.Combine(DataRoot, "Backup")); }
        }

        public static string Temp
        {
            get { return EnsureDirectory(Path.Combine(DataRoot, "Temp")); }
        }

        public static string BackupFor(string applicationDirectory)
        {
            string normalized = Path.GetFullPath(applicationDirectory)
                .TrimEnd(Path.DirectorySeparatorChar)
                .ToUpperInvariant();
            byte[] bytes = Encoding.UTF8.GetBytes(normalized);
            byte[] hash;
            using (SHA256 sha = SHA256.Create())
                hash = sha.ComputeHash(bytes);
            string key = BitConverter.ToString(hash)
                .Replace("-", string.Empty)
                .Substring(0, 20);
            return Path.Combine(Backups, key);
        }

        private static string SelectWritableDataRoot()
        {
            string executableDirectory = Path.GetDirectoryName(
                Process.GetCurrentProcess().MainModule.FileName);
            string portable = Path.Combine(executableDirectory, "Data");
            if (CanUseDirectory(portable))
                return portable;

            string common = Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData);
            string fallback = Path.Combine(common, "QC20倍率切换工具");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        private static bool CanUseDirectory(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                string probe = Path.Combine(
                    path,
                    ".write-probe-" + Guid.NewGuid().ToString("N"));
                using (FileStream stream = new FileStream(
                    probe,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                    stream.WriteByte(0x51);
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string EnsureDirectory(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
