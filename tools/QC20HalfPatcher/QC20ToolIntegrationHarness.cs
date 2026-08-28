using System;
using System.Globalization;
using System.IO;
using Wataa.QC20Tool;

internal static class QC20ToolIntegrationHarness
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 2)
            {
                Console.Error.WriteLine(
                    "Usage: QC20ToolIntegrationHarness " +
                    "<original backup directory> <test root>");
                return 2;
            }

            string originals = Path.GetFullPath(args[0]);
            string testRoot = Path.GetFullPath(args[1]);
            string application = Path.Combine(
                testRoot, "Target", "Application");
            Directory.CreateDirectory(application);
            File.Copy(
                Path.Combine(originals, AppInfo.BallbarFile),
                Path.Combine(application, AppInfo.BallbarFile),
                false);
            File.Copy(
                Path.Combine(originals, AppInfo.DeviceServiceFile),
                Path.Combine(application, AppInfo.DeviceServiceFile),
                false);

            BackupSet backup =
                BackupService.EnsureOriginalBackup(application);
            double[] factors = { 0.01, 0.50, 5.00 };
            foreach (double factor in factors)
            {
                PatchedPair pair =
                    Qc20PatchService.Create(backup, factor);
                try
                {
                    string factorDirectory = Path.Combine(
                        testRoot,
                        "Factor_" + factor.ToString(
                            "0.00", CultureInfo.InvariantCulture));
                    Directory.CreateDirectory(factorDirectory);
                    File.Copy(
                        pair.BallbarPath,
                        Path.Combine(
                            factorDirectory, AppInfo.BallbarFile),
                        true);
                    File.Copy(
                        pair.DeviceServicePath,
                        Path.Combine(
                            factorDirectory,
                            AppInfo.DeviceServiceFile),
                        true);

                    BackupService.ReplacePairWithRollback(
                        application,
                        pair.BallbarPath,
                        pair.DeviceServicePath,
                        pair.BallbarHash,
                        pair.DeviceServiceHash);
                    CurrentStateService.Write(
                        application,
                        factor,
                        pair.BallbarHash,
                        pair.DeviceServiceHash);
                    string state =
                        CurrentStateService.Describe(application);
                    Console.WriteLine(
                        "FACTOR={0}|STATE={1}|BALLBAR={2}|DEVICE={3}",
                        factor.ToString(
                            "R", CultureInfo.InvariantCulture),
                        state,
                        pair.BallbarHash,
                        pair.DeviceServiceHash);
                }
                finally
                {
                    Qc20PatchService.Delete(pair);
                }
            }

            string manualOutput = Path.Combine(
                testRoot, "ManualOutput");
            Directory.CreateDirectory(manualOutput);
            string package = ManualPackageService.Generate(
                manualOutput, backup, 5.0);
            bool packagePass =
                File.Exists(Path.Combine(
                    package,
                    "01_修改版_复制到Application",
                    AppInfo.BallbarFile)) &&
                File.Exists(Path.Combine(
                    package,
                    "01_修改版_复制到Application",
                    AppInfo.DeviceServiceFile)) &&
                File.Exists(Path.Combine(
                    package,
                    "02_原版备份_用于恢复",
                    AppInfo.BallbarFile)) &&
                File.Exists(Path.Combine(
                    package,
                    "SHA256SUMS.txt"));
            Console.WriteLine(
                "MANUAL_PACKAGE_PASS=" + packagePass);

            BackupService.Restore(application, backup);
            CurrentStateService.MarkOriginal(application);
            bool restorePass =
                FileIntegrity.EqualsHash(
                    Path.Combine(
                        application, AppInfo.BallbarFile),
                    AppInfo.OriginalBallbarHash) &&
                FileIntegrity.EqualsHash(
                    Path.Combine(
                        application,
                        AppInfo.DeviceServiceFile),
                    AppInfo.OriginalDeviceServiceHash);
            Console.WriteLine("RESTORE_PASS=" + restorePass);
            Console.WriteLine(
                "FINAL_STATE=" +
                CurrentStateService.Describe(application));
            Console.WriteLine("TEST_ROOT=" + testRoot);
            return packagePass && restorePass ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                exception.GetType().FullName + ": " +
                exception.Message);
            Console.Error.WriteLine(exception.StackTrace);
            return 1;
        }
    }
}
