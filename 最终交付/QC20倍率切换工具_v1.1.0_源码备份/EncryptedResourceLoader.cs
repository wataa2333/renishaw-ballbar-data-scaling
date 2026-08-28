using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Wataa.QC20Tool
{
    internal static class EncryptedResourceLoader
    {
        private const string Prefix = "Wataa.QC20Tool.Resources.";
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedAssembly;
        }

        public static byte[] Read(string resourceName)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(
                Prefix + resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        "缺少加密资源：" + resourceName);
                byte[] encrypted = new byte[stream.Length];
                int read = 0;
                while (read < encrypted.Length)
                {
                    int count = stream.Read(
                        encrypted, read, encrypted.Length - read);
                    if (count <= 0)
                        throw new EndOfStreamException(resourceName);
                    read += count;
                }
                return Decrypt(encrypted);
            }
        }

        private static Assembly ResolveEmbeddedAssembly(
            object sender,
            ResolveEventArgs arguments)
        {
            AssemblyName requested = new AssemblyName(arguments.Name);
            if (!string.Equals(
                    requested.Name,
                    "dnlib",
                    StringComparison.OrdinalIgnoreCase))
                return null;
            byte[] bytes = Read("dnlib.enc");
            try
            {
                return Assembly.Load(bytes);
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        private static byte[] Decrypt(byte[] package)
        {
            if (package == null || package.Length < 4 + 16 + 32)
                throw new CryptographicException("加密资源格式无效。");
            if (package[0] != (byte)'Q' ||
                package[1] != (byte)'C' ||
                package[2] != (byte)'R' ||
                package[3] != (byte)'1')
                throw new CryptographicException("加密资源标识无效。");

            int authenticatedLength = package.Length - 32;
            byte[] expectedMac = new byte[32];
            Buffer.BlockCopy(
                package, authenticatedLength, expectedMac, 0, 32);

            byte[] aesKey;
            byte[] macKey;
            DeriveKeys(out aesKey, out macKey);
            try
            {
                byte[] actualMac;
                using (HMACSHA256 hmac = new HMACSHA256(macKey))
                    actualMac = hmac.ComputeHash(
                        package, 0, authenticatedLength);
                bool valid = ConstantTimeEquals(actualMac, expectedMac);
                Array.Clear(actualMac, 0, actualMac.Length);
                Array.Clear(expectedMac, 0, expectedMac.Length);
                if (!valid)
                    throw new CryptographicException(
                        "加密资源完整性校验失败。");

                byte[] iv = new byte[16];
                Buffer.BlockCopy(package, 4, iv, 0, iv.Length);
                int cipherLength = authenticatedLength - 20;
                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 256;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key = aesKey;
                    aes.IV = iv;
                    using (ICryptoTransform decryptor =
                        aes.CreateDecryptor())
                    {
                        return decryptor.TransformFinalBlock(
                            package, 20, cipherLength);
                    }
                }
            }
            finally
            {
                Array.Clear(aesKey, 0, aesKey.Length);
                Array.Clear(macKey, 0, macKey.Length);
            }
        }

        private static void DeriveKeys(
            out byte[] aesKey,
            out byte[] macKey)
        {
            byte[] secret = SecretVault.GetKeyBytes();
            byte[] context = Encoding.UTF8.GetBytes(
                "Wataa.QC20.Resource.v1");
            byte[] combined = new byte[secret.Length + context.Length];
            Buffer.BlockCopy(secret, 0, combined, 0, secret.Length);
            Buffer.BlockCopy(
                context, 0, combined, secret.Length, context.Length);
            byte[] digest;
            using (SHA512 sha = SHA512.Create())
                digest = sha.ComputeHash(combined);
            aesKey = new byte[32];
            macKey = new byte[32];
            Buffer.BlockCopy(digest, 0, aesKey, 0, 32);
            Buffer.BlockCopy(digest, 32, macKey, 0, 32);
            Array.Clear(secret, 0, secret.Length);
            Array.Clear(combined, 0, combined.Length);
            Array.Clear(digest, 0, digest.Length);
        }

        private static bool ConstantTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null)
                return false;
            int difference = left.Length ^ right.Length;
            int count = Math.Min(left.Length, right.Length);
            for (int index = 0; index < count; index++)
                difference |= left[index] ^ right[index];
            return difference == 0;
        }
    }
}
