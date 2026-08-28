using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wataa.QC20Tool
{
    internal enum WpfButtonKind
    {
        Neutral,
        Primary,
        Danger,
        Positive,
        Segment,
        SegmentSelected
    }

    internal static class WpfUi
    {
        public const double ControlCornerRadius = 10;
        public const double CardCornerRadius = 10;
        private static BitmapImage avatar;

        public static TextBlock Text(
            string value,
            double size,
            Brush color)
        {
            TextBlock text = new TextBlock();
            text.Text = value;
            text.FontFamily = WpfTheme.UiFont;
            text.FontSize = size;
            text.Foreground = color;
            text.TextWrapping = TextWrapping.Wrap;
            text.VerticalAlignment = VerticalAlignment.Center;
            return text;
        }

        public static Button Button(
            string text,
            WpfButtonKind kind,
            double minWidth)
        {
            Button button = new Button();
            button.Content = text;
            button.MinWidth = minWidth;
            bool isSegment =
                kind == WpfButtonKind.Segment ||
                kind == WpfButtonKind.SegmentSelected;
            button.MinHeight = isSegment
                ? 34
                : (kind == WpfButtonKind.Neutral ? 38 : 43);
            button.Padding = isSegment
                ? new Thickness(14, 5, 14, 5)
                : new Thickness(16, 8, 16, 8);
            button.FontFamily = WpfTheme.UiFont;
            button.FontSize = 14;
            button.FontWeight = FontWeights.Medium;
            button.Cursor = System.Windows.Input.Cursors.Hand;
            ApplyButtonKind(button, kind);
            return button;
        }

        public static void ApplyButtonKind(
            Button button,
            WpfButtonKind kind)
        {
            button.Style = CreateButtonStyle(kind);
        }

        public static Border Card(UIElement child)
        {
            Border border = new Border();
            border.Background = WpfTheme.Surface;
            border.BorderBrush = WpfTheme.Border;
            border.BorderThickness = new Thickness(1);
            border.CornerRadius =
                new CornerRadius(CardCornerRadius);
            border.Padding = new Thickness(12);
            border.Margin = new Thickness(0, 0, 0, 8);
            border.Child = child;
            border.Effect = new DropShadowEffect
            {
                BlurRadius = 14,
                ShadowDepth = 2,
                Opacity = WpfTheme.IsDark ? 0.20 : 0.08,
                Color = Colors.Black
            };
            return border;
        }

        public static Ellipse Avatar(double size)
        {
            Ellipse ellipse = new Ellipse();
            ellipse.Width = size;
            ellipse.Height = size;
            ellipse.Stroke = Brushes.White;
            ellipse.StrokeThickness = size >= 80 ? 3 : 2;
            ellipse.Fill = new ImageBrush(GetAvatar())
            {
                Stretch = Stretch.UniformToFill,
                AlignmentX = AlignmentX.Center,
                AlignmentY = AlignmentY.Center
            };
            ellipse.Effect = new DropShadowEffect
            {
                BlurRadius = 13,
                ShadowDepth = 3,
                Opacity = 0.22,
                Color = Colors.Black
            };
            return ellipse;
        }

        public static TextBox TextBox()
        {
            TextBox box = new TextBox();
            box.FontFamily = WpfTheme.UiFont;
            box.FontSize = 14;
            box.Foreground = WpfTheme.Text;
            box.Background = WpfTheme.SurfaceAlt;
            box.BorderBrush = WpfTheme.Border;
            box.BorderThickness = new Thickness(1);
            box.Padding = new Thickness(11, 8, 11, 8);
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.Style = CreateInputStyle(typeof(TextBox));
            return box;
        }

        public static PasswordBox PasswordBox()
        {
            PasswordBox box = new PasswordBox();
            box.FontFamily = WpfTheme.UiFont;
            box.FontSize = 14;
            box.Foreground = WpfTheme.Text;
            box.Background = WpfTheme.SurfaceAlt;
            box.BorderBrush = WpfTheme.Border;
            box.BorderThickness = new Thickness(1);
            box.Padding = new Thickness(11, 8, 11, 8);
            box.VerticalContentAlignment = VerticalAlignment.Center;
            box.Style = CreateInputStyle(typeof(PasswordBox));
            return box;
        }

        public static ImageSource ApplicationIcon()
        {
            return GetAvatar();
        }

        private static Style CreateInputStyle(Type targetType)
        {
            Style style = new Style(targetType);

            FrameworkElementFactory frame =
                new FrameworkElementFactory(typeof(Border));
            frame.SetValue(
                Border.CornerRadiusProperty,
                new CornerRadius(ControlCornerRadius));
            frame.SetValue(
                Border.BackgroundProperty,
                new TemplateBindingExtension(
                    Control.BackgroundProperty));
            frame.SetValue(
                Border.BorderBrushProperty,
                new TemplateBindingExtension(
                    Control.BorderBrushProperty));
            frame.SetValue(
                Border.BorderThicknessProperty,
                new TemplateBindingExtension(
                    Control.BorderThicknessProperty));
            frame.SetValue(
                Border.PaddingProperty,
                new TemplateBindingExtension(
                    Control.PaddingProperty));

            FrameworkElementFactory host =
                new FrameworkElementFactory(typeof(ScrollViewer));
            host.Name = "PART_ContentHost";
            host.SetValue(
                FrameworkElement.HorizontalAlignmentProperty,
                HorizontalAlignment.Stretch);
            host.SetValue(
                FrameworkElement.VerticalAlignmentProperty,
                VerticalAlignment.Stretch);
            frame.AppendChild(host);

            ControlTemplate template =
                new ControlTemplate(targetType);
            template.VisualTree = frame;

            Trigger focusTrigger = new Trigger();
            focusTrigger.Property =
                UIElement.IsKeyboardFocusedProperty;
            focusTrigger.Value = true;
            focusTrigger.Setters.Add(
                new Setter(
                    Control.BorderBrushProperty,
                    WpfTheme.Blue));
            template.Triggers.Add(focusTrigger);

            style.Setters.Add(
                new Setter(Control.TemplateProperty, template));
            return style;
        }

        private static Style CreateButtonStyle(WpfButtonKind kind)
        {
            Brush background;
            Brush hover;
            Brush foreground;
            Brush border;
            if (kind == WpfButtonKind.Primary)
            {
                background = WpfTheme.Blue;
                hover = WpfTheme.BlueHover;
                foreground = Brushes.White;
                border = WpfTheme.Blue;
            }
            else if (kind == WpfButtonKind.Danger)
            {
                background = WpfTheme.Red;
                hover = WpfTheme.RedHover;
                foreground = Brushes.White;
                border = WpfTheme.Red;
            }
            else if (kind == WpfButtonKind.Positive)
            {
                background = WpfTheme.Green;
                hover = WpfTheme.GreenHover;
                foreground = Brushes.White;
                border = WpfTheme.Green;
            }
            else if (kind == WpfButtonKind.SegmentSelected)
            {
                background = Brushes.White;
                hover = Brushes.White;
                foreground = WpfTheme.Text;
                border = WpfTheme.Border;
            }
            else if (kind == WpfButtonKind.Segment)
            {
                background = Brushes.Transparent;
                hover = WpfTheme.Surface;
                foreground = WpfTheme.Text;
                border = Brushes.Transparent;
            }
            else
            {
                background = WpfTheme.SurfaceAlt;
                hover = WpfTheme.Surface;
                foreground = WpfTheme.Text;
                border = WpfTheme.Border;
            }

            Style style = new Style(typeof(Button));
            style.Setters.Add(
                new Setter(Control.BackgroundProperty, background));
            style.Setters.Add(
                new Setter(Control.ForegroundProperty, foreground));
            style.Setters.Add(
                new Setter(Control.BorderBrushProperty, border));
            style.Setters.Add(
                new Setter(Control.BorderThicknessProperty, new Thickness(1)));

            FrameworkElementFactory frame =
                new FrameworkElementFactory(typeof(Border));
            frame.SetValue(
                Border.CornerRadiusProperty,
                new CornerRadius(ControlCornerRadius));
            frame.SetValue(
                Border.BackgroundProperty,
                new TemplateBindingExtension(
                    Control.BackgroundProperty));
            frame.SetValue(
                Border.BorderBrushProperty,
                new TemplateBindingExtension(
                    Control.BorderBrushProperty));
            frame.SetValue(
                Border.BorderThicknessProperty,
                new TemplateBindingExtension(
                    Control.BorderThicknessProperty));
            frame.SetValue(
                Border.PaddingProperty,
                new TemplateBindingExtension(
                    Control.PaddingProperty));

            FrameworkElementFactory presenter =
                new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(
                FrameworkElement.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);
            presenter.SetValue(
                FrameworkElement.VerticalAlignmentProperty,
                VerticalAlignment.Center);
            presenter.SetValue(
                TextElement.ForegroundProperty,
                new TemplateBindingExtension(
                    Control.ForegroundProperty));
            frame.AppendChild(presenter);

            ControlTemplate template =
                new ControlTemplate(typeof(Button));
            template.VisualTree = frame;
            style.Setters.Add(
                new Setter(Control.TemplateProperty, template));

            Trigger hoverTrigger = new Trigger();
            hoverTrigger.Property = UIElement.IsMouseOverProperty;
            hoverTrigger.Value = true;
            hoverTrigger.Setters.Add(
                new Setter(Control.BackgroundProperty, hover));
            style.Triggers.Add(hoverTrigger);

            Trigger pressedTrigger = new Trigger();
            pressedTrigger.Property = ButtonBase.IsPressedProperty;
            pressedTrigger.Value = true;
            pressedTrigger.Setters.Add(
                new Setter(UIElement.OpacityProperty, 0.82));
            style.Triggers.Add(pressedTrigger);

            Trigger disabledTrigger = new Trigger();
            disabledTrigger.Property = UIElement.IsEnabledProperty;
            disabledTrigger.Value = false;
            disabledTrigger.Setters.Add(
                new Setter(UIElement.OpacityProperty, 0.45));
            style.Triggers.Add(disabledTrigger);
            return style;
        }

        private static BitmapImage GetAvatar()
        {
            if (avatar != null)
                return avatar;
            byte[] bytes =
                EncryptedResourceLoader.Read("avatar.enc");
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    avatar = image;
                }
                return avatar;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }
    }
}
