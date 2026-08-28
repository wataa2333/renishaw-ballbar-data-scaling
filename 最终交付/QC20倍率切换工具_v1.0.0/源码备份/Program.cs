using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Wataa.QC20Tool
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            EncryptedResourceLoader.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string initialTarget = ReadTargetArgument(args);
            try
            {
                Logger.Info(
                    "程序启动，版本=" + AppInfo.Version +
                    "，管理员=" + PermissionService.IsAdministrator());
                using (LoginForm login = new LoginForm())
                {
                    if (login.ShowDialog() != DialogResult.OK)
                        return;
                }
                Application.Run(new MainForm(initialTarget));
            }
            catch (Exception exception)
            {
                Logger.Error("程序发生未处理异常", exception);
                MessageBox.Show(
                    "程序发生错误：" + exception.Message,
                    AppInfo.ShortName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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
