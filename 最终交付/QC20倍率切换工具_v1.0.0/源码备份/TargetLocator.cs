using System;
using System.IO;

namespace Wataa.QC20Tool
{
    internal static class TargetLocator
    {
        public static string ResolveApplicationDirectory(string selectedPath)
        {
            if (string.IsNullOrWhiteSpace(selectedPath))
                throw new InvalidOperationException("请选择软件目录。");

            string root = Path.GetFullPath(
                Environment.ExpandEnvironmentVariables(selectedPath.Trim()));
            if (ContainsTargets(root))
                return root;

            string application = Path.Combine(root, "Application");
            if (ContainsTargets(application))
                return application;

            throw new DirectoryNotFoundException(
                "所选位置中没有找到 Application 目录及两份目标 DLL。");
        }

        public static bool ContainsTargets(string directory)
        {
            return Directory.Exists(directory) &&
                File.Exists(Path.Combine(
                    directory, AppInfo.BallbarFile)) &&
                File.Exists(Path.Combine(
                    directory, AppInfo.DeviceServiceFile));
        }
    }
}
