using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Wataa.QC20Tool
{
    internal sealed class CurrentState
    {
        public double Factor;
        public string BallbarHash;
        public string DeviceHash;
    }

    internal static class CurrentStateService
    {
        public static string Describe(string applicationDirectory)
        {
            string ballbar = Path.Combine(
                applicationDirectory, AppInfo.BallbarFile);
            string device = Path.Combine(
                applicationDirectory, AppInfo.DeviceServiceFile);
            if (FileIntegrity.EqualsHash(
                    ballbar, AppInfo.OriginalBallbarHash) &&
                FileIntegrity.EqualsHash(
                    device, AppInfo.OriginalDeviceServiceHash))
                return "原版 100%（×1.00）";

            CurrentState state = Read(applicationDirectory);
            if (state != null &&
                FileIntegrity.EqualsHash(
                    ballbar, state.BallbarHash) &&
                FileIntegrity.EqualsHash(
                    device, state.DeviceHash))
                return FormatFactor(state.Factor);
            return "未知或不匹配";
        }

        public static void Write(
            string applicationDirectory,
            double factor,
            string ballbarHash,
            string deviceHash)
        {
            string backupDirectory = AppPaths.BackupFor(
                applicationDirectory);
            Directory.CreateDirectory(backupDirectory);
            string content = string.Join(
                Environment.NewLine,
                new string[]
                {
                    "Format=QC20StateV1",
                    "Factor=" + factor.ToString(
                        "R", CultureInfo.InvariantCulture),
                    "BallbarSHA256=" + ballbarHash,
                    "DeviceSHA256=" + deviceHash,
                    "Updated=" + DateTime.UtcNow.ToString(
                        "o", CultureInfo.InvariantCulture)
                });
            File.WriteAllText(
                Path.Combine(backupDirectory, "current.state.txt"),
                content,
                new UTF8Encoding(false));
        }

        public static void MarkOriginal(string applicationDirectory)
        {
            Write(
                applicationDirectory,
                1.0,
                AppInfo.OriginalBallbarHash,
                AppInfo.OriginalDeviceServiceHash);
        }

        private static CurrentState Read(string applicationDirectory)
        {
            string path = Path.Combine(
                AppPaths.BackupFor(applicationDirectory),
                "current.state.txt");
            if (!File.Exists(path))
                return null;
            try
            {
                CurrentState state = new CurrentState();
                foreach (string line in File.ReadAllLines(path))
                {
                    int separator = line.IndexOf('=');
                    if (separator <= 0)
                        continue;
                    string key = line.Substring(0, separator);
                    string value = line.Substring(separator + 1);
                    if (key == "Factor")
                        state.Factor = double.Parse(
                            value, CultureInfo.InvariantCulture);
                    else if (key == "BallbarSHA256")
                        state.BallbarHash = value;
                    else if (key == "DeviceSHA256")
                        state.DeviceHash = value;
                }
                if (state.Factor <= 0 ||
                    string.IsNullOrEmpty(state.BallbarHash) ||
                    string.IsNullOrEmpty(state.DeviceHash))
                    return null;
                return state;
            }
            catch
            {
                return null;
            }
        }

        public static string FormatFactor(double factor)
        {
            double percent = factor * 100.0;
            return percent.ToString(
                percent == Math.Round(percent) ? "0" : "0.##",
                CultureInfo.InvariantCulture) +
                "%（×" +
                factor.ToString("0.##", CultureInfo.InvariantCulture) +
                "）";
        }
    }
}
