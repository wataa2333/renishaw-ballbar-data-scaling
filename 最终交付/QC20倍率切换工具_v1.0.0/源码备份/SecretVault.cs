using System;

namespace Wataa.QC20Tool
{
    // The TOTP key is never stored as a readable Base32 string. These two
    // fragments reconstruct the decoded key only for the shortest possible
    // time. This raises extraction cost but cannot make an offline key
    // impossible to recover from a program running on an attacker's computer.
    internal static class SecretVault
    {
        private static readonly byte[] FragmentA =
        {
            0x9E, 0x63, 0x1F, 0x0C, 0x27,
            0xBE, 0x19, 0x5A, 0x18, 0x6E
        };

        private static readonly byte[] FragmentB =
        {
            0xA3, 0x5C, 0x91, 0x27, 0xE8,
            0x4D, 0x36, 0xB2, 0x7F, 0x19
        };

        public static byte[] GetKeyBytes()
        {
            byte[] result = new byte[FragmentA.Length];
            for (int index = 0; index < result.Length; index++)
                result[index] = (byte)(FragmentA[index] ^ FragmentB[index]);
            return result;
        }

        public static void Clear(byte[] bytes)
        {
            if (bytes != null)
                Array.Clear(bytes, 0, bytes.Length);
        }
    }
}
