using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace Wataa.QC20Tool
{
    internal static class ManualPackageService
    {
        public static string Generate(
            string outputRoot,
            BackupSet originals,
            double factor)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
                throw new InvalidOperationException("请选择输出目录。");
            string label = (factor * 100.0).ToString(
                "0.##", CultureInfo.InvariantCulture);
            string root = Path.Combine(
                outputRoot,
                "QC20_倍率" + label + "%_手动替换包_" +
                DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            string modified = Path.Combine(
                root, "01_修改版_复制到Application");
            string recovery = Path.Combine(
                root, "02_原版备份_用于恢复");
            Directory.CreateDirectory(modified);
            Directory.CreateDirectory(recovery);

            PatchedPair pair = null;
            try
            {
                string modifiedBallbar =
                    Path.Combine(modified, AppInfo.BallbarFile);
                string modifiedDevice =
                    Path.Combine(modified, AppInfo.DeviceServiceFile);

                if (Math.Abs(factor - 1.0) < 0.0000000001)
                {
                    File.Copy(
                        originals.BallbarPath,
                        modifiedBallbar,
                        false);
                    File.Copy(
                        originals.DeviceServicePath,
                        modifiedDevice,
                        false);
                }
                else
                {
                    pair = Qc20PatchService.Create(originals, factor);
                    File.Copy(
                        pair.BallbarPath,
                        modifiedBallbar,
                        false);
                    File.Copy(
                        pair.DeviceServicePath,
                        modifiedDevice,
                        false);
                }

                File.Copy(
                    originals.BallbarPath,
                    Path.Combine(recovery, AppInfo.BallbarFile),
                    false);
                File.Copy(
                    originals.DeviceServicePath,
                    Path.Combine(recovery, AppInfo.DeviceServiceFile),
                    false);

                string modifiedBallbarHash =
                    FileIntegrity.Sha256(modifiedBallbar);
                string modifiedDeviceHash =
                    FileIntegrity.Sha256(modifiedDevice);
                string sums =
                    modifiedBallbarHash +
                    "  01_修改版_复制到Application/" +
                    AppInfo.BallbarFile + Environment.NewLine +
                    modifiedDeviceHash +
                    "  01_修改版_复制到Application/" +
                    AppInfo.DeviceServiceFile + Environment.NewLine +
                    AppInfo.OriginalBallbarHash +
                    "  02_原版备份_用于恢复/" +
                    AppInfo.BallbarFile + Environment.NewLine +
                    AppInfo.OriginalDeviceServiceHash +
                    "  02_原版备份_用于恢复/" +
                    AppInfo.DeviceServiceFile + Environment.NewLine;
                File.WriteAllText(
                    Path.Combine(root, "SHA256SUMS.txt"),
                    sums,
                    new UTF8Encoding(false));

                string instructions =
                    "QC20 手动替换包" + Environment.NewLine +
                    "倍率：" + label + "%（×" +
                    factor.ToString(
                        "0.##", CultureInfo.InvariantCulture) +
                    "）" + Environment.NewLine + Environment.NewLine +
                    "1. 完全退出 Renishaw Ballbar 20。" +
                    Environment.NewLine +
                    "2. 将 01 文件夹中的两份 DLL 同时复制到 " +
                    "Application 目录。" + Environment.NewLine +
                    "3. 不得与其他倍率的 DLL 混用。" +
                    Environment.NewLine +
                    "4. 回退时将 02 文件夹中的两份 DLL 同时复制回去。" +
                    Environment.NewLine + Environment.NewLine +
                    "仅限学习研究测试，不用于正式计量或验收。" +
                    Environment.NewLine;
                File.WriteAllText(
                    Path.Combine(root, "手动替换说明.txt"),
                    instructions,
                    new UTF8Encoding(false));
                Logger.Info(
                    "已生成手动替换包，倍率=" + label +
                    "%，输出=" + root);
                return root;
            }
            finally
            {
                Qc20PatchService.Delete(pair);
            }
        }
    }
}
