using System;
using System.Drawing;
using Microsoft.Win32;

namespace Wataa.QC20Tool
{
    internal static class Theme
    {
        public static readonly bool IsDark = ReadDarkMode();

        public static Color Window
        {
            get { return IsDark ? Color.FromArgb(31, 35, 40) : Color.FromArgb(246, 249, 252); }
        }

        public static Color Surface
        {
            get { return IsDark ? Color.FromArgb(43, 48, 54) : Color.White; }
        }

        public static Color SurfaceAlt
        {
            get { return IsDark ? Color.FromArgb(37, 42, 47) : Color.FromArgb(238, 243, 247); }
        }

        public static Color Text
        {
            get { return IsDark ? Color.FromArgb(242, 244, 246) : Color.FromArgb(31, 35, 39); }
        }

        public static Color Muted
        {
            get { return IsDark ? Color.FromArgb(180, 187, 195) : Color.FromArgb(93, 102, 111); }
        }

        public static Color Border
        {
            get { return IsDark ? Color.FromArgb(72, 79, 87) : Color.FromArgb(210, 218, 225); }
        }

        public static Color Blue
        {
            get { return Color.FromArgb(35, 124, 185); }
        }

        public static Color Red
        {
            get { return Color.FromArgb(199, 48, 48); }
        }

        public static Color Green
        {
            get { return Color.FromArgb(39, 139, 95); }
        }

        private static bool ReadDarkMode()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    object value = key == null
                        ? null
                        : key.GetValue("AppsUseLightTheme");
                    return value is int && (int)value == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
