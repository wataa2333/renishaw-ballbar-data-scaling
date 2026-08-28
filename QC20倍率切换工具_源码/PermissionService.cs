using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;

namespace Wataa.QC20Tool
{
    internal static class PermissionService
    {
        public static bool CanWriteDirectory(string directory)
        {
            try
            {
                string probe = Path.Combine(
                    directory,
                    ".qc20-write-probe-" + Guid.NewGuid().ToString("N"));
                using (FileStream stream = new FileStream(
                    probe,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None))
                    stream.WriteByte(0x51);
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity identity =
                    WindowsIdentity.GetCurrent();
                WindowsPrincipal principal =
                    new WindowsPrincipal(identity);
                return principal.IsInRole(
                    WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static bool RestartElevated(string targetDirectory)
        {
            try
            {
                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName =
                    Process.GetCurrentProcess().MainModule.FileName;
                start.Arguments = "--target \"" +
                    targetDirectory.Replace("\"", "\\\"") + "\"";
                start.UseShellExecute = true;
                start.Verb = "runas";
                Process.Start(start);
                return true;
            }
            catch (Win32Exception exception)
            {
                if (exception.NativeErrorCode == 1223)
                    return false;
                throw;
            }
        }
    }
}
