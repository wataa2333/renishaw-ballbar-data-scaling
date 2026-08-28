using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Wataa.QC20Tool
{
    internal sealed class BackupSet
    {
        public string DirectoryPath;
        public string BallbarPath;
        public string DeviceServicePath;
    }

    internal static class BackupService
    {
        public static BackupSet EnsureOriginalBackup(
            string applicationDirectory)
        {
            BackupSet existing = GetExisting(applicationDirectory);
            if (existing != null)
                return existing;

            string targetBallbar = Path.Combine(
                applicationDirectory, AppInfo.BallbarFile);
            string targetDevice = Path.Combine(
                applicationDirectory, AppInfo.DeviceServiceFile);

            if (!FileIntegrity.EqualsHash(
                    targetBallbar, AppInfo.OriginalBallbarHash) ||
                !FileIntegrity.EqualsHash(
                    targetDevice, AppInfo.OriginalDeviceServiceHash))
                throw new InvalidOperationException(
                    "首次备份要求目标目录是已核验的原版文件。" +
                    "当前文件哈希未知，已停止操作，防止把修改版当作原版备份。");

            string backupDirectory = AppPaths.BackupFor(
                applicationDirectory);
            Directory.CreateDirectory(backupDirectory);
            string backupBallbar = Path.Combine(
                backupDirectory, AppInfo.BallbarFile);
            string backupDevice = Path.Combine(
                backupDirectory, AppInfo.DeviceServiceFile);

            File.Copy(targetBallbar, backupBallbar, false);
            File.Copy(targetDevice, backupDevice, false);
            ValidateOriginals(backupBallbar, backupDevice);

            string manifest = string.Join(
                Environment.NewLine,
                new string[]
                {
                    "Format=QC20BackupV1",
                    "Created=" + DateTime.UtcNow.ToString(
                        "o", CultureInfo.InvariantCulture),
                    "ApplicationDirectory=" + applicationDirectory,
                    "BallbarSHA256=" + AppInfo.OriginalBallbarHash,
                    "DeviceServiceSHA256=" +
                        AppInfo.OriginalDeviceServiceHash
                });
            File.WriteAllText(
                Path.Combine(backupDirectory, "backup.manifest.txt"),
                manifest,
                new UTF8Encoding(false));
            Logger.Info(
                "已创建原版备份，目标目录=" + applicationDirectory +
                "，备份目录=" + backupDirectory);

            return NewSet(
                backupDirectory, backupBallbar, backupDevice);
        }

        public static BackupSet GetExisting(
            string applicationDirectory)
        {
            string directory = AppPaths.BackupFor(applicationDirectory);
            string ballbar = Path.Combine(
                directory, AppInfo.BallbarFile);
            string device = Path.Combine(
                directory, AppInfo.DeviceServiceFile);
            if (!File.Exists(ballbar) || !File.Exists(device))
                return null;
            ValidateOriginals(ballbar, device);
            return NewSet(directory, ballbar, device);
        }

        public static void Restore(
            string applicationDirectory,
            BackupSet backup)
        {
            if (backup == null)
                throw new InvalidOperationException("没有可用的原版备份。");
            ValidateOriginals(
                backup.BallbarPath, backup.DeviceServicePath);
            ReplacePairWithRollback(
                applicationDirectory,
                backup.BallbarPath,
                backup.DeviceServicePath,
                AppInfo.OriginalBallbarHash,
                AppInfo.OriginalDeviceServiceHash);
            Logger.Info(
                "已恢复原版，目标目录=" + applicationDirectory);
        }

        public static void ReplacePairWithRollback(
            string applicationDirectory,
            string newBallbar,
            string newDevice,
            string expectedBallbarHash,
            string expectedDeviceHash)
        {
            string targetBallbar = Path.Combine(
                applicationDirectory, AppInfo.BallbarFile);
            string targetDevice = Path.Combine(
                applicationDirectory, AppInfo.DeviceServiceFile);
            string rollbackDirectory = Path.Combine(
                AppPaths.Temp,
                "rollback-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(rollbackDirectory);
            string rollbackBallbar = Path.Combine(
                rollbackDirectory, AppInfo.BallbarFile);
            string rollbackDevice = Path.Combine(
                rollbackDirectory, AppInfo.DeviceServiceFile);
            File.Copy(targetBallbar, rollbackBallbar, true);
            File.Copy(targetDevice, rollbackDevice, true);

            try
            {
                File.Copy(newBallbar, targetBallbar, true);
                File.Copy(newDevice, targetDevice, true);
                if (!FileIntegrity.EqualsHash(
                        targetBallbar, expectedBallbarHash) ||
                    !FileIntegrity.EqualsHash(
                        targetDevice, expectedDeviceHash))
                    throw new IOException("替换后的 SHA-256 校验失败。");
            }
            catch
            {
                try
                {
                    File.Copy(rollbackBallbar, targetBallbar, true);
                    File.Copy(rollbackDevice, targetDevice, true);
                }
                catch (Exception rollbackError)
                {
                    Logger.Error("自动回滚失败", rollbackError);
                }
                throw;
            }
            finally
            {
                TryDeleteDirectory(rollbackDirectory);
            }
        }

        private static void ValidateOriginals(
            string ballbar,
            string device)
        {
            if (!FileIntegrity.EqualsHash(
                    ballbar, AppInfo.OriginalBallbarHash) ||
                !FileIntegrity.EqualsHash(
                    device, AppInfo.OriginalDeviceServiceHash))
                throw new InvalidOperationException(
                    "备份文件完整性校验失败，拒绝继续。");
        }

        private static BackupSet NewSet(
            string directory,
            string ballbar,
            string device)
        {
            BackupSet set = new BackupSet();
            set.DirectoryPath = directory;
            set.BallbarPath = ballbar;
            set.DeviceServicePath = device;
            return set;
        }

        private static void TryDeleteDirectory(string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch
            {
            }
        }
    }
}
