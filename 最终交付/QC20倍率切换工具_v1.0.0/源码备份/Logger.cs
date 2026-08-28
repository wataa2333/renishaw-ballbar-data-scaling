using System;
using System.IO;
using System.Text;

namespace Wataa.QC20Tool
{
    internal static class Logger
    {
        private static readonly object Sync = new object();

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message, Exception exception)
        {
            string detail = message;
            if (exception != null)
                detail += " | " + exception.GetType().FullName +
                    ": " + exception.Message;
            Write("ERROR", detail);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    string file = Path.Combine(
                        AppPaths.Logs,
                        "QC20操作日志_" +
                        DateTime.Now.ToString("yyyyMMdd") +
                        ".log");
                    string line = string.Format(
                        "{0:yyyy-MM-dd HH:mm:ss.fff} [{1}] {2}{3}",
                        DateTime.Now,
                        level,
                        Sanitize(message),
                        Environment.NewLine);
                    File.AppendAllText(file, line, new UTF8Encoding(false));
                }
            }
            catch
            {
                // Logging must never make recovery or rollback fail.
            }
        }

        private static string Sanitize(string message)
        {
            if (message == null)
                return string.Empty;
            return message
                .Replace("\r", " ")
                .Replace("\n", " ");
        }
    }
}
