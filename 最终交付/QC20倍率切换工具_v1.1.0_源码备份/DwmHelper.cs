using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Wataa.QC20Tool
{
    internal static class DwmHelper
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int value,
            int size);

        public static void Apply(Window window)
        {
            try
            {
                WindowInteropHelper helper =
                    new WindowInteropHelper(window);
                IntPtr handle = helper.Handle;

                HwndSource source = HwndSource.FromHwnd(handle);
                if (source != null && source.CompositionTarget != null)
                    source.CompositionTarget.BackgroundColor =
                        Colors.Transparent;

                int dark = WpfTheme.IsDark ? 1 : 0;
                DwmSetWindowAttribute(
                    handle, 20, ref dark, sizeof(int));

                int corner = 2;
                DwmSetWindowAttribute(
                    handle, 33, ref corner, sizeof(int));

                int backdrop = 2;
                DwmSetWindowAttribute(
                    handle, 38, ref backdrop, sizeof(int));
            }
            catch
            {
            }
        }
    }
}
