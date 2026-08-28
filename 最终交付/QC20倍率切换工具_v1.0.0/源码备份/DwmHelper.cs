using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        public static void Apply(Form form)
        {
            try
            {
                int dark = Theme.IsDark ? 1 : 0;
                DwmSetWindowAttribute(
                    form.Handle, 20, ref dark, sizeof(int));

                // Windows 11 rounded corners.
                int corner = 2;
                DwmSetWindowAttribute(
                    form.Handle, 33, ref corner, sizeof(int));

                // Windows 11 Mica system backdrop. Older Windows versions
                // safely ignore an unsupported attribute.
                int backdrop = 2;
                DwmSetWindowAttribute(
                    form.Handle, 38, ref backdrop, sizeof(int));
            }
            catch
            {
                // DWM decoration is optional; functionality remains intact.
            }
        }
    }
}
