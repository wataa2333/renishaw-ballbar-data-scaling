using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Wataa.QC20Tool
{
    internal sealed class LoginWindow : Window
    {
        private readonly PasswordBox codeBox;
        private readonly TextBlock errorText;
        private readonly Button verifyButton;
        private readonly DispatcherTimer lockTimer;
        private int failedAttempts;
        private DateTime lockedUntil;

        public LoginWindow()
        {
            Title = AppInfo.ProductName;
            Width = 480;
            Height = 540;
            MinWidth = 480;
            MinHeight = 540;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            Icon = WpfUi.ApplicationIcon();
            FontFamily = WpfTheme.UiFont;
            Foreground = WpfTheme.Text;

            Grid root = new Grid();
            root.Background = WpfTheme.Mica;
            root.Margin = new Thickness(0);
            Content = root;

            StackPanel stack = new StackPanel();
            stack.Width = 390;
            stack.HorizontalAlignment = HorizontalAlignment.Center;
            stack.VerticalAlignment = VerticalAlignment.Center;
            root.Children.Add(stack);

            StackPanel identity = new StackPanel();
            identity.Orientation = Orientation.Horizontal;
            identity.Margin = new Thickness(0, 0, 0, 24);
            FrameworkElement avatar = WpfUi.Avatar(64);
            identity.Children.Add(avatar);
            StackPanel identityText = new StackPanel();
            identityText.Margin = new Thickness(14, 18, 0, 0);
            TextBlock user = WpfUi.Text(
                "wataa", 18, WpfTheme.Text);
            user.FontWeight = FontWeights.Medium;
            identityText.Children.Add(user);
            identity.Children.Add(identityText);
            stack.Children.Add(identity);

            TextBlock heading = WpfUi.Text(
                "验证动态口令", 25, WpfTheme.Text);
            heading.FontWeight = FontWeights.Bold;
            heading.Margin = new Thickness(0, 0, 0, 8);
            stack.Children.Add(heading);

            TextBlock copy = WpfUi.Text(
                "请输入 Google Authenticator 当前显示的 6 位动态口令",
                14,
                WpfTheme.Muted);
            copy.Margin = new Thickness(0, 0, 0, 20);
            stack.Children.Add(copy);

            codeBox = WpfUi.PasswordBox();
            codeBox.MaxLength = 6;
            codeBox.Height = 48;
            codeBox.FontSize = 22;
            codeBox.HorizontalContentAlignment =
                HorizontalAlignment.Center;
            codeBox.VerticalContentAlignment =
                VerticalAlignment.Center;
            codeBox.Padding = new Thickness(12, 6, 12, 6);
            codeBox.Margin = new Thickness(0, 0, 0, 13);
            codeBox.PreviewTextInput += CodePreviewTextInput;
            codeBox.KeyDown += CodeKeyDown;
            stack.Children.Add(codeBox);

            Border metaSurface = new Border();
            metaSurface.Background = WpfTheme.SurfaceAlt;
            metaSurface.CornerRadius = new CornerRadius(8);
            metaSurface.Padding = new Thickness(12, 9, 12, 9);
            metaSurface.Margin = new Thickness(0, 0, 0, 19);
            TextBlock meta = WpfUi.Text(
                "◷  30 秒更新                         ●  支持离线验证",
                12,
                WpfTheme.Muted);
            meta.TextAlignment = TextAlignment.Left;
            metaSurface.Child = meta;
            stack.Children.Add(metaSurface);

            verifyButton = WpfUi.Button(
                "验证并进入工具",
                WpfButtonKind.Primary,
                260);
            verifyButton.Height = 48;
            verifyButton.HorizontalAlignment =
                HorizontalAlignment.Stretch;
            verifyButton.Click += VerifyClick;
            stack.Children.Add(verifyButton);

            errorText = WpfUi.Text(
                string.Empty, 12, WpfTheme.Red);
            errorText.TextAlignment = TextAlignment.Center;
            errorText.MinHeight = 38;
            errorText.Margin = new Thickness(0, 10, 0, 2);
            stack.Children.Add(errorText);

            TextBlock footer = WpfUi.Text(
                "仅限学习研究测试  ·  制作人：wataa" +
                Environment.NewLine +
                AppInfo.Email,
                12,
                WpfTheme.Muted);
            footer.TextAlignment = TextAlignment.Left;
            footer.HorizontalAlignment = HorizontalAlignment.Left;
            stack.Children.Add(footer);

            lockTimer = new DispatcherTimer();
            lockTimer.Interval = TimeSpan.FromSeconds(1);
            lockTimer.Tick += LockTimerTick;

            Loaded += delegate
            {
                DwmHelper.Apply(this);
                codeBox.Focus();
            };
        }

        private void VerifyClick(
            object sender,
            RoutedEventArgs eventArgs)
        {
            if (DateTime.UtcNow < lockedUntil)
                return;

            if (TotpService.Verify(codeBox.Password))
            {
                Logger.Info("TOTP 动态口令验证成功");
                DialogResult = true;
                return;
            }

            failedAttempts++;
            codeBox.Clear();
            if (failedAttempts >= 5)
            {
                lockedUntil = DateTime.UtcNow.AddSeconds(60);
                failedAttempts = 0;
                verifyButton.IsEnabled = false;
                lockTimer.Start();
                UpdateLockText();
                Logger.Info(
                    "TOTP 连续失败 5 次，临时锁定 60 秒");
            }
            else
            {
                errorText.Text =
                    "动态口令不正确，还可尝试 " +
                    (5 - failedAttempts) +
                    " 次。请检查手机和电脑时间。";
                Logger.Info("TOTP 动态口令验证失败");
            }
        }

        private void LockTimerTick(
            object sender,
            EventArgs eventArgs)
        {
            if (DateTime.UtcNow >= lockedUntil)
            {
                lockTimer.Stop();
                verifyButton.IsEnabled = true;
                errorText.Text = string.Empty;
                codeBox.Focus();
            }
            else
                UpdateLockText();
        }

        private void UpdateLockText()
        {
            int seconds = (int)Math.Ceiling(
                (lockedUntil - DateTime.UtcNow).TotalSeconds);
            errorText.Text =
                "尝试次数过多，请等待 " + seconds + " 秒。";
        }

        private static void CodePreviewTextInput(
            object sender,
            TextCompositionEventArgs eventArgs)
        {
            foreach (char value in eventArgs.Text)
            {
                if (!char.IsDigit(value))
                {
                    eventArgs.Handled = true;
                    return;
                }
            }
        }

        private void CodeKeyDown(
            object sender,
            KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.Enter)
            {
                VerifyClick(sender, eventArgs);
                eventArgs.Handled = true;
            }
        }
    }
}
