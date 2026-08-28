using System;
using System.Windows;

namespace Wataa.QC20Tool
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EncryptedResourceLoader.Initialize();
            string initialTarget = ReadTargetArgument(args);
            try
            {
                Logger.Info(
                    "程序启动，版本=" + AppInfo.Version +
                    "，管理员=" + PermissionService.IsAdministrator());
                Application application = new Application();
                application.ShutdownMode =
                    ShutdownMode.OnExplicitShutdown;
                LoginWindow login = new LoginWindow();
                bool? accepted = login.ShowDialog();
                if (accepted != true)
                {
                    application.Shutdown();
                    return;
                }

                MainWindow main = new MainWindow(initialTarget);
                application.MainWindow = main;
                application.ShutdownMode =
                    ShutdownMode.OnMainWindowClose;
                application.Run(main);
            }
            catch (Exception exception)
            {
                Logger.Error("程序发生未处理异常", exception);
                MessageBox.Show(
                    "程序发生错误：" + exception.Message,
                    AppInfo.ShortName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string ReadTargetArgument(string[] args)
        {
            for (int index = 0; index + 1 < args.Length; index++)
            {
                if (string.Equals(
                        args[index],
                        "--target",
                        StringComparison.OrdinalIgnoreCase))
                    return args[index + 1];
            }
            return null;
        }
    }
}
