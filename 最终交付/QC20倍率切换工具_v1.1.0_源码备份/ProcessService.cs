using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Wataa.QC20Tool
{
    internal sealed class RunningProcessInfo
    {
        public int Id;
        public string Name;
        public string Title;
    }

    internal static class ProcessService
    {
        public static List<RunningProcessInfo> FindBallbarProcesses()
        {
            List<RunningProcessInfo> result =
                new List<RunningProcessInfo>();
            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                return result;
            }

            foreach (Process process in processes)
            {
                try
                {
                    string name = process.ProcessName ?? string.Empty;
                    string title = process.MainWindowTitle ?? string.Empty;
                    bool nameMatch =
                        name.IndexOf(
                            "Renishaw Ballbar",
                            StringComparison.OrdinalIgnoreCase) >= 0;
                    bool titleMatch =
                        title.IndexOf(
                            "Renishaw Ballbar",
                            StringComparison.OrdinalIgnoreCase) >= 0;
                    if (nameMatch || titleMatch)
                    {
                        RunningProcessInfo info =
                            new RunningProcessInfo();
                        info.Id = process.Id;
                        info.Name = name;
                        info.Title = title;
                        result.Add(info);
                    }
                }
                catch
                {
                }
                finally
                {
                    process.Dispose();
                }
            }
            return result;
        }

        public static void StopAll(
            IList<RunningProcessInfo> processes)
        {
            foreach (RunningProcessInfo info in processes)
            {
                using (Process process = Process.GetProcessById(info.Id))
                {
                    process.Kill();
                    if (!process.WaitForExit(10000))
                        throw new InvalidOperationException(
                            "进程未在 10 秒内结束，PID=" + info.Id);
                }
                Logger.Info(
                    "已按用户确认结束 Renishaw 进程，PID=" + info.Id);
            }
        }
    }
}
