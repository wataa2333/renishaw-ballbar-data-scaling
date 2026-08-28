using System;
using System.IO;
using System.Security.Cryptography;

namespace Wataa.QC20Tool
{
    internal static class FileIntegrity
    {
        public static string Sha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read))
                return BitConverter.ToString(
                    sha.ComputeHash(stream)).Replace("-", string.Empty);
        }

        public static bool EqualsHash(string path, string expected)
        {
            return File.Exists(path) &&
                string.Equals(
                    Sha256(path),
                    expected,
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
