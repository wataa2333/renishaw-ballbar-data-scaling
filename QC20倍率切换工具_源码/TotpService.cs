using System;
using System.Globalization;
using System.Security.Cryptography;

namespace Wataa.QC20Tool
{
    internal static class TotpService
    {
        private const int PeriodSeconds = 30;
        private const int Digits = 6;

        public static bool Verify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            string trimmed = input.Trim();
            if (trimmed.Length != Digits)
                return false;
            for (int index = 0; index < trimmed.Length; index++)
            {
                if (!char.IsDigit(trimmed[index]))
                    return false;
            }

            long counter = GetCurrentCounter();
            byte[] key = SecretVault.GetKeyBytes();
            try
            {
                // One previous and one following window tolerate modest clock
                // drift while retaining a short acceptance period.
                for (long offset = -1; offset <= 1; offset++)
                {
                    string expected = Generate(key, counter + offset);
                    if (FixedTimeEquals(expected, trimmed))
                        return true;
                }
                return false;
            }
            finally
            {
                SecretVault.Clear(key);
            }
        }

        internal static string CurrentCodeForSelfTest(DateTime utcNow)
        {
            long seconds = (long)(utcNow - new DateTime(
                1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
            byte[] key = SecretVault.GetKeyBytes();
            try
            {
                return Generate(key, seconds / PeriodSeconds);
            }
            finally
            {
                SecretVault.Clear(key);
            }
        }

        private static long GetCurrentCounter()
        {
            DateTime epoch = new DateTime(
                1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long seconds = (long)(DateTime.UtcNow - epoch).TotalSeconds;
            return seconds / PeriodSeconds;
        }

        private static string Generate(byte[] key, long counter)
        {
            byte[] message = BitConverter.GetBytes(counter);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(message);

            byte[] hash;
            using (HMACSHA1 hmac = new HMACSHA1(key))
                hash = hmac.ComputeHash(message);

            int offset = hash[hash.Length - 1] & 0x0F;
            int binary =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);
            int otp = binary % 1000000;
            Array.Clear(hash, 0, hash.Length);
            return otp.ToString("D6", CultureInfo.InvariantCulture);
        }

        private static bool FixedTimeEquals(string left, string right)
        {
            int difference = left.Length ^ right.Length;
            int count = Math.Min(left.Length, right.Length);
            for (int index = 0; index < count; index++)
                difference |= left[index] ^ right[index];
            return difference == 0;
        }
    }
}
