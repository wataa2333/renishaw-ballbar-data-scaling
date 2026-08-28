using System.Windows;
using System.Windows.Media;

namespace Wataa.QC20Tool
{
    internal static class WpfTheme
    {
        // The tool deliberately uses one predictable light palette so its
        // contrast and spacing remain identical on Windows 10 and Windows 11.
        public const bool IsDark = false;
        public static readonly FontFamily UiFont =
            new FontFamily("Microsoft YaHei UI");

        public static readonly Brush Window =
            Brush(248, 250, 252);
        public static readonly Brush Mica =
            Brush(248, 250, 252);
        public static readonly Brush Surface =
            Brush(255, 255, 255);
        public static readonly Brush SurfaceAlt =
            Brush(244, 247, 250);
        public static readonly Brush Text =
            Brush(32, 33, 36);
        public static readonly Brush Muted =
            Brush(92, 103, 114);
        public static readonly Brush Border =
            Brush(206, 219, 230);
        public static readonly Brush Blue = Brush(40, 132, 194);
        public static readonly Brush BlueHover = Brush(51, 148, 211);
        public static readonly Brush Red = Brush(199, 48, 48);
        public static readonly Brush RedHover = Brush(218, 62, 62);
        public static readonly Brush Green = Brush(35, 143, 94);
        public static readonly Brush GreenHover = Brush(45, 160, 108);
        public static readonly Brush SuccessSurface =
            BrushWithAlpha(44, 39, 160, 91);
        public static readonly Brush DangerSurface =
            BrushWithAlpha(38, 199, 48, 48);

        private static Brush Brush(int red, int green, int blue)
        {
            SolidColorBrush brush =
                new SolidColorBrush(Color.FromRgb(
                    (byte)red, (byte)green, (byte)blue));
            brush.Freeze();
            return brush;
        }

        private static Brush BrushWithAlpha(
            byte alpha,
            byte red,
            byte green,
            byte blue)
        {
            SolidColorBrush brush = new SolidColorBrush(
                Color.FromArgb(alpha, red, green, blue));
            brush.Freeze();
            return brush;
        }

    }
}
