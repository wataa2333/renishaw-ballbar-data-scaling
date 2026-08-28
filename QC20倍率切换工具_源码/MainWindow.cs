using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace Wataa.QC20Tool
{
    internal sealed class MainWindow : Window
    {
        private readonly TextBox targetBox;
        private readonly TextBlock statusText;
        private readonly TextBlock selectedText;
        private readonly TextBlock explanationText;
        private readonly TextBox manualPercentBox;
        private readonly StackPanel manualMode;
        private readonly UniformGrid presetMode;
        private readonly Button manualTab;
        private readonly Button presetTab;
        private readonly List<Button> presetButtons;
        private bool usingManual;
        private double selectedPercent = 50.0;

        private static readonly int[] Presets =
        {
            1, 5, 10, 20, 25, 30, 40, 50, 60,
            75, 80, 90, 200, 250, 300, 400, 500
        };

        public MainWindow(string initialTarget)
        {
            Title = AppInfo.ProductName;
            Width = 640;
            Height = 650;
            MinWidth = 640;
            MinHeight = 650;
            ResizeMode = ResizeMode.CanMinimize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            Icon = WpfUi.ApplicationIcon();
            FontFamily = WpfTheme.UiFont;
            Foreground = WpfTheme.Text;

            Grid root = new Grid();
            root.Background = WpfTheme.Mica;
            Content = root;

            Border contentHost = new Border();
            contentHost.Padding = new Thickness(16, 12, 16, 10);
            root.Children.Add(contentHost);

            Viewbox pageScaler = new Viewbox();
            pageScaler.Stretch = Stretch.Uniform;
            pageScaler.StretchDirection = StretchDirection.DownOnly;
            pageScaler.HorizontalAlignment = HorizontalAlignment.Stretch;
            pageScaler.VerticalAlignment = VerticalAlignment.Stretch;
            contentHost.Child = pageScaler;

            StackPanel page = new StackPanel();
            page.Width = 640;
            page.HorizontalAlignment = HorizontalAlignment.Center;
            page.VerticalAlignment = VerticalAlignment.Top;
            pageScaler.Child = page;

            Grid header = new Grid();
            header.Margin = new Thickness(0, 0, 0, 8);
            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            header.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });
            StackPanel headingStack = new StackPanel();
            headingStack.VerticalAlignment = VerticalAlignment.Center;
            TextBlock heading = WpfUi.Text(
                "位移偏差倍率", 38, WpfTheme.Text);
            heading.FontWeight = FontWeights.Bold;
            headingStack.Children.Add(heading);
            TextBlock subtitle = WpfUi.Text(
                "选择目标目录并设置实时与采集数据倍率",
                17,
                WpfTheme.Muted);
            subtitle.Margin = new Thickness(0, 5, 0, 0);
            headingStack.Children.Add(subtitle);

            header.Children.Add(headingStack);

            StackPanel headerRight = new StackPanel();
            headerRight.HorizontalAlignment = HorizontalAlignment.Right;
            StackPanel identity = new StackPanel();
            identity.Orientation = Orientation.Horizontal;
            identity.HorizontalAlignment = HorizontalAlignment.Right;
            FrameworkElement headerAvatar = WpfUi.Avatar(75);
            identity.Children.Add(headerAvatar);
            StackPanel identityText = new StackPanel();
            identityText.Margin = new Thickness(11, 11, 0, 0);
            TextBlock headerUser = WpfUi.Text(
                "wataa", 18, WpfTheme.Text);
            headerUser.FontWeight = FontWeights.Medium;
            identityText.Children.Add(headerUser);
            TextBlock verified = WpfUi.Text(
                "动态口令已验证", 13, WpfTheme.Green);
            identityText.Children.Add(verified);
            identity.Children.Add(identityText);
            headerRight.Children.Add(identity);

            Border statusPill = new Border();
            statusPill.Background = WpfTheme.SuccessSurface;
            statusPill.CornerRadius =
                new CornerRadius(WpfUi.ControlCornerRadius);
            statusPill.Padding = new Thickness(10, 5, 10, 5);
            statusPill.HorizontalAlignment = HorizontalAlignment.Stretch;
            statusPill.Margin = new Thickness(0, 6, 0, 0);
            statusText = WpfUi.Text(
                "●  等待检测", 12, WpfTheme.Green);
            statusText.FontWeight = FontWeights.Medium;
            statusText.TextAlignment = TextAlignment.Center;
            statusPill.Child = statusText;
            headerRight.Children.Add(statusPill);
            Grid.SetColumn(headerRight, 1);
            header.Children.Add(headerRight);
            page.Children.Add(header);

            StackPanel directoryContent = new StackPanel();
            directoryContent.Children.Add(
                CardTitle("Renishaw Ballbar 20 软件位置"));
            Grid pathRow = new Grid();
            pathRow.Margin = new Thickness(0, 8, 0, 0);
            pathRow.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            pathRow.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });
            targetBox = WpfUi.TextBox();
            targetBox.Text = string.IsNullOrEmpty(initialTarget)
                ? DefaultTarget()
                : initialTarget;
            targetBox.MinHeight = 38;
            pathRow.Children.Add(targetBox);

            Button browse = WpfUi.Button(
                "浏览…", WpfButtonKind.Neutral, 92);
            browse.Margin = new Thickness(10, 0, 0, 0);
            browse.Click += BrowseTarget;
            Grid.SetColumn(browse, 1);
            pathRow.Children.Add(browse);

            directoryContent.Children.Add(pathRow);
            page.Children.Add(WpfUi.Card(directoryContent));

            StackPanel factorContent = new StackPanel();
            factorContent.Children.Add(
                CardTitle("选择目标倍率"));

            StackPanel tabRow = new StackPanel();
            tabRow.Orientation = Orientation.Horizontal;
            manualTab = WpfUi.Button(
                "手动输入", WpfButtonKind.Segment, 112);
            manualTab.Click += delegate { SetMode(true); };
            tabRow.Children.Add(manualTab);
            presetTab = WpfUi.Button(
                "预设列表", WpfButtonKind.SegmentSelected, 112);
            presetTab.Margin = new Thickness(3, 0, 0, 0);
            presetTab.Click += delegate { SetMode(false); };
            tabRow.Children.Add(presetTab);
            Border segmented = new Border();
            segmented.Background = WpfTheme.SurfaceAlt;
            segmented.CornerRadius =
                new CornerRadius(WpfUi.ControlCornerRadius);
            segmented.Padding = new Thickness(3);
            segmented.Margin = new Thickness(0, 6, 0, 6);
            segmented.HorizontalAlignment = HorizontalAlignment.Left;
            segmented.Child = tabRow;
            factorContent.Children.Add(segmented);

            Grid modeHost = new Grid();
            modeHost.Height = 126;
            modeHost.VerticalAlignment = VerticalAlignment.Top;

            manualMode = new StackPanel();
            manualMode.Visibility = Visibility.Collapsed;
            manualMode.VerticalAlignment = VerticalAlignment.Center;
            TextBlock manualLabel = WpfUi.Text(
                "输入百分比（允许范围 1%–500%）",
                13,
                WpfTheme.Muted);
            manualMode.Children.Add(manualLabel);
            Grid manualRow = new Grid();
            manualRow.Margin = new Thickness(0, 9, 0, 7);
            manualRow.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(190)
                });
            manualRow.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });
            manualPercentBox = WpfUi.TextBox();
            manualPercentBox.Text = "50";
            manualPercentBox.FontSize = 18;
            manualPercentBox.PreviewTextInput +=
                ManualPreviewTextInput;
            manualPercentBox.TextChanged +=
                ManualTextChanged;
            manualRow.Children.Add(manualPercentBox);
            TextBlock percentSymbol = WpfUi.Text(
                "%", 17, WpfTheme.Muted);
            percentSymbol.Margin = new Thickness(10, 0, 0, 0);
            Grid.SetColumn(percentSymbol, 1);
            manualRow.Children.Add(percentSymbol);
            manualMode.Children.Add(manualRow);
            modeHost.Children.Add(manualMode);

            presetMode = new UniformGrid();
            presetMode.Columns = 6;
            presetMode.VerticalAlignment = VerticalAlignment.Top;
            presetMode.Margin = new Thickness(0, 0, 0, 4);
            presetButtons = new List<Button>();
            foreach (int preset in Presets)
            {
                Button button = WpfUi.Button(
                    preset + "%",
                    preset == 50
                    ? WpfButtonKind.Primary
                        : WpfButtonKind.Neutral,
                    72);
                button.Margin = new Thickness(0, 0, 7, 6);
                button.MinHeight = 34;
                button.Height = 34;
                button.Tag = preset;
                button.Click += PresetClick;
                presetButtons.Add(button);
                presetMode.Children.Add(button);
            }
            modeHost.Children.Add(presetMode);
            factorContent.Children.Add(modeHost);

            Border selectionSurface = new Border();
            selectionSurface.Background = WpfTheme.SurfaceAlt;
            selectionSurface.CornerRadius =
                new CornerRadius(WpfUi.ControlCornerRadius);
            selectionSurface.Padding = new Thickness(12, 7, 12, 7);
            selectionSurface.Margin = new Thickness(0, 4, 0, 0);
            Grid selectionGrid = new Grid();
            selectionGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            selectionGrid.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                });
            selectedText = WpfUi.Text(
                "当前选择：50%（×0.50）",
                15,
                WpfTheme.Text);
            selectedText.FontWeight = FontWeights.Medium;
            selectionGrid.Children.Add(selectedText);
            explanationText = WpfUi.Text(
                "位移偏差数据缩小为原值的 50%",
                13,
                WpfTheme.Muted);
            explanationText.Margin = new Thickness(14, 0, 0, 0);
            Grid.SetColumn(explanationText, 1);
            selectionGrid.Children.Add(explanationText);
            selectionSurface.Child = selectionGrid;
            factorContent.Children.Add(selectionSurface);

            page.Children.Add(WpfUi.Card(factorContent));

            Grid actions = new Grid();
            actions.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });
            actions.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            actions.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });

            StackPanel leftActions = new StackPanel();
            leftActions.Orientation = Orientation.Horizontal;
            leftActions.HorizontalAlignment =
                HorizontalAlignment.Center;
            Button backupButton = WpfUi.Button(
                "原版备份", WpfButtonKind.Neutral, 92);
            backupButton.Click += delegate
            {
                OpenDirectory(AppPaths.Backups);
            };
            leftActions.Children.Add(backupButton);
            Button logButton = WpfUi.Button(
                "操作日志", WpfButtonKind.Neutral, 92);
            logButton.Margin = new Thickness(7, 0, 0, 0);
            logButton.Click += delegate
            {
                OpenDirectory(AppPaths.Logs);
            };
            leftActions.Children.Add(logButton);
            Button package = WpfUi.Button(
                "生成手动替换包",
                WpfButtonKind.Positive,
                144);
            package.Margin = new Thickness(7, 0, 0, 0);
            package.Click += GenerateManualPackage;
            leftActions.Children.Add(package);
            actions.Children.Add(leftActions);

            StackPanel rightActions = new StackPanel();
            rightActions.Orientation = Orientation.Horizontal;
            rightActions.HorizontalAlignment =
                HorizontalAlignment.Center;
            Button restore = WpfUi.Button(
                "恢复原版",
                WpfButtonKind.Danger,
                112);
            restore.Click += RestoreOriginal;
            rightActions.Children.Add(restore);

            Button apply = WpfUi.Button(
                "应用所选倍率",
                WpfButtonKind.Primary,
                134);
            apply.Margin = new Thickness(7, 0, 0, 0);
            apply.Click += ApplyFactor;
            rightActions.Children.Add(apply);
            Grid.SetColumn(rightActions, 2);
            actions.Children.Add(rightActions);
            page.Children.Add(WpfUi.Card(actions));

            Grid footer = new Grid();
            footer.Margin = new Thickness(3, 1, 3, 0);
            footer.ColumnDefinitions.Add(
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                });
            footer.ColumnDefinitions.Add(
                new ColumnDefinition { Width = GridLength.Auto });
            TextBlock dataLocation = WpfUi.Text(
                "备份与日志：EXE目录\\Data",
                11,
                WpfTheme.Muted);
            dataLocation.VerticalAlignment = VerticalAlignment.Bottom;
            footer.Children.Add(dataLocation);
            TextBlock owner = WpfUi.Text(
                "仅限学习研究测试，不用于正式计量或验收" +
                Environment.NewLine +
                "制作人：wataa  ·  邮箱：" + AppInfo.Email,
                11,
                WpfTheme.Muted);
            owner.TextAlignment = TextAlignment.Right;
            Grid.SetColumn(owner, 1);
            footer.Children.Add(owner);
            page.Children.Add(footer);

            Loaded += delegate
            {
                DwmHelper.Apply(this);
                RefreshTargetState(false);
            };
        }

        private Border BuildSidebar()
        {
            Border sidebar = new Border();
            sidebar.Background = WpfTheme.SurfaceAlt;
            sidebar.BorderBrush = WpfTheme.Border;
            sidebar.BorderThickness = new Thickness(0, 0, 1, 0);
            sidebar.Padding = new Thickness(16, 24, 16, 18);

            Grid layout = new Grid();
            layout.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });
            layout.RowDefinitions.Add(
                new RowDefinition
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
            layout.RowDefinitions.Add(
                new RowDefinition { Height = GridLength.Auto });
            sidebar.Child = layout;

            StackPanel identity = new StackPanel();
            identity.Orientation = Orientation.Horizontal;
            FrameworkElement avatar = WpfUi.Avatar(58);
            identity.Children.Add(avatar);
            StackPanel identityText = new StackPanel();
            identityText.Margin = new Thickness(12, 7, 0, 0);
            TextBlock user = WpfUi.Text(
                "wataa", 15, WpfTheme.Text);
            user.FontWeight = FontWeights.Medium;
            identityText.Children.Add(user);
            TextBlock verified = WpfUi.Text(
                "● 动态口令已验证", 11, WpfTheme.Green);
            verified.Margin = new Thickness(0, 3, 0, 0);
            identityText.Children.Add(verified);
            identity.Children.Add(identityText);
            layout.Children.Add(identity);

            StackPanel nav = new StackPanel();
            nav.Margin = new Thickness(0, 28, 0, 0);
            Button switchButton = WpfUi.Button(
                "◈  倍率切换",
                WpfButtonKind.Primary,
                188);
            switchButton.HorizontalContentAlignment =
                HorizontalAlignment.Left;
            nav.Children.Add(switchButton);

            Button backupButton = WpfUi.Button(
                "↺  查看原版备份",
                WpfButtonKind.Neutral,
                188);
            backupButton.Margin = new Thickness(0, 9, 0, 0);
            backupButton.HorizontalContentAlignment =
                HorizontalAlignment.Left;
            backupButton.Click += delegate
            {
                OpenDirectory(AppPaths.Backups);
            };
            nav.Children.Add(backupButton);

            Button logButton = WpfUi.Button(
                "≡  打开操作日志",
                WpfButtonKind.Neutral,
                188);
            logButton.Margin = new Thickness(0, 9, 0, 0);
            logButton.HorizontalContentAlignment =
                HorizontalAlignment.Left;
            logButton.Click += delegate
            {
                OpenDirectory(AppPaths.Logs);
            };
            nav.Children.Add(logButton);
            Grid.SetRow(nav, 1);
            layout.Children.Add(nav);

            TextBlock version = WpfUi.Text(
                "v" + AppInfo.Version,
                11,
                WpfTheme.Muted);
            version.VerticalAlignment = VerticalAlignment.Bottom;
            Grid.SetRow(version, 3);
            layout.Children.Add(version);
            return sidebar;
        }

        private void SetMode(bool manual)
        {
            usingManual = manual;
            manualMode.Visibility = manual
                ? Visibility.Visible
                : Visibility.Collapsed;
            presetMode.Visibility = manual
                ? Visibility.Collapsed
                : Visibility.Visible;
            WpfUi.ApplyButtonKind(
                manualTab,
                manual
                    ? WpfButtonKind.SegmentSelected
                    : WpfButtonKind.Segment);
            WpfUi.ApplyButtonKind(
                presetTab,
                manual
                    ? WpfButtonKind.Segment
                    : WpfButtonKind.SegmentSelected);
            if (manual)
                ReadManualPercent();
            UpdateSelection();
        }

        private void PresetClick(
            object sender,
            RoutedEventArgs eventArgs)
        {
            Button clicked = (Button)sender;
            selectedPercent = Convert.ToDouble(clicked.Tag);
            foreach (Button button in presetButtons)
                WpfUi.ApplyButtonKind(
                    button,
                    ReferenceEquals(button, clicked)
                        ? WpfButtonKind.Primary
                        : WpfButtonKind.Neutral);
            UpdateSelection();
        }

        private void ManualTextChanged(
            object sender,
            TextChangedEventArgs eventArgs)
        {
            if (usingManual)
            {
                ReadManualPercent();
                UpdateSelection();
            }
        }

        private static void ManualPreviewTextInput(
            object sender,
            TextCompositionEventArgs eventArgs)
        {
            double ignored;
            eventArgs.Handled = !double.TryParse(
                eventArgs.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out ignored);
        }

        private void ReadManualPercent()
        {
            double value;
            if (!double.TryParse(
                    manualPercentBox.Text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out value))
                return;
            if (value < 1)
                value = 1;
            if (value > 500)
                value = 500;
            selectedPercent = value;
        }

        private void UpdateSelection()
        {
            double factor = selectedPercent / 100.0;
            selectedText.Text =
                "当前选择：" +
                selectedPercent.ToString(
                    "0.##", CultureInfo.InvariantCulture) +
                "%（×" +
                factor.ToString(
                    "0.00", CultureInfo.InvariantCulture) +
                "）";
            if (selectedPercent < 100)
                explanationText.Text =
                    "位移偏差数据缩小为原值的 " +
                    selectedPercent.ToString(
                        "0.##", CultureInfo.InvariantCulture) +
                    "%";
            else if (Math.Abs(selectedPercent - 100) < 0.0001)
                explanationText.Text =
                    "位移偏差数据保持原值";
            else
                explanationText.Text =
                    "位移偏差数据放大为原值的 " +
                    factor.ToString(
                        "0.##", CultureInfo.InvariantCulture) +
                    " 倍";
        }

        private void ApplyFactor(
            object sender,
            RoutedEventArgs eventArgs)
        {
            RunOperation(
                delegate
                {
                    string application = ResolveTarget();
                    if (!EnsureWritePermission(application))
                        return;
                    if (!ConfirmAndStopProcesses())
                        return;
                    BackupSet backup =
                        BackupService.EnsureOriginalBackup(application);
                    double factor = selectedPercent / 100.0;
                    if (Math.Abs(factor - 1.0) < 0.0000000001)
                    {
                        BackupService.Restore(application, backup);
                        CurrentStateService.MarkOriginal(application);
                    }
                    else
                    {
                        PatchedPair pair =
                            Qc20PatchService.Create(backup, factor);
                        try
                        {
                            BackupService.ReplacePairWithRollback(
                                application,
                                pair.BallbarPath,
                                pair.DeviceServicePath,
                                pair.BallbarHash,
                                pair.DeviceServiceHash);
                            CurrentStateService.Write(
                                application,
                                factor,
                                pair.BallbarHash,
                                pair.DeviceServiceHash);
                            Logger.Info(
                                "已应用倍率=" +
                                factor.ToString(
                                    "R",
                                    CultureInfo.InvariantCulture) +
                                "，目标目录=" + application);
                        }
                        finally
                        {
                            Qc20PatchService.Delete(pair);
                        }
                    }
                    RefreshTargetState(false);
                    MessageBox.Show(
                        this,
                        "两份 DLL 已同时处理并通过 SHA-256 校验。",
                        AppInfo.ShortName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
        }

        private void RestoreOriginal(
            object sender,
            RoutedEventArgs eventArgs)
        {
            if (MessageBox.Show(
                    this,
                    "将同时恢复两份原版 DLL。是否继续？",
                    "恢复原版",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;
            RunOperation(
                delegate
                {
                    string application = ResolveTarget();
                    if (!EnsureWritePermission(application))
                        return;
                    if (!ConfirmAndStopProcesses())
                        return;
                    BackupSet backup =
                        BackupService.GetExisting(application);
                    if (backup == null)
                        throw new InvalidOperationException(
                            "没有找到该目录对应的可靠原版备份。");
                    BackupService.Restore(application, backup);
                    CurrentStateService.MarkOriginal(application);
                    RefreshTargetState(false);
                    MessageBox.Show(
                        this,
                        "原版已恢复并通过 SHA-256 校验。",
                        AppInfo.ShortName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                });
        }

        private void GenerateManualPackage(
            object sender,
            RoutedEventArgs eventArgs)
        {
            RunOperation(
                delegate
                {
                    string application = ResolveTarget();
                    BackupSet backup =
                        BackupService.EnsureOriginalBackup(application);
                    using (System.Windows.Forms.FolderBrowserDialog dialog =
                        new System.Windows.Forms.FolderBrowserDialog())
                    {
                        dialog.Description =
                            "选择手动替换包的输出位置";
                        if (dialog.ShowDialog() !=
                            System.Windows.Forms.DialogResult.OK)
                            return;
                        string output = ManualPackageService.Generate(
                            dialog.SelectedPath,
                            backup,
                            selectedPercent / 100.0);
                        MessageBox.Show(
                            this,
                            "手动替换包已生成：" +
                            Environment.NewLine + output,
                            AppInfo.ShortName,
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        OpenDirectory(output);
                    }
                });
        }

        private string ResolveTarget()
        {
            return TargetLocator.ResolveApplicationDirectory(
                targetBox.Text);
        }

        private bool EnsureWritePermission(string application)
        {
            if (PermissionService.CanWriteDirectory(application))
                return true;
            if (PermissionService.IsAdministrator())
                throw new UnauthorizedAccessException(
                    "即使在管理员权限下，该目录仍不可写。");

            MessageBoxResult answer = MessageBox.Show(
                this,
                "当前用户没有目标目录写入权限。" +
                Environment.NewLine +
                "是否以管理员身份重新启动？重新启动后需要再次输入动态口令。",
                "需要管理员权限",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
                return false;
            if (PermissionService.RestartElevated(targetBox.Text))
            {
                Logger.Info("用户同意以管理员身份重新启动");
                Application.Current.Shutdown();
            }
            return false;
        }

        private bool ConfirmAndStopProcesses()
        {
            List<RunningProcessInfo> processes =
                ProcessService.FindBallbarProcesses();
            if (processes.Count == 0)
                return true;

            StringBuilder details = new StringBuilder();
            foreach (RunningProcessInfo process in processes)
                details.AppendLine(
                    process.Name + "  PID " + process.Id);

            MessageBoxResult answer = MessageBox.Show(
                this,
                "检测到 Renishaw Ballbar 20 正在运行：" +
                Environment.NewLine + Environment.NewLine +
                details.ToString() + Environment.NewLine +
                "必须结束这些进程后才能安全替换 DLL。" +
                "是否结束进程并继续？",
                "软件正在运行",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (answer != MessageBoxResult.Yes)
                return false;
            ProcessService.StopAll(processes);
            return true;
        }

        private void RefreshTargetState(bool showErrors)
        {
            try
            {
                string application = ResolveTarget();
                string state =
                    CurrentStateService.Describe(application);
                statusText.Text = "●  " + state;
                statusText.Foreground =
                    state == "未知或不匹配"
                    ? WpfTheme.Red
                    : WpfTheme.Green;
            }
            catch (Exception exception)
            {
                statusText.Text = "●  目录无效";
                statusText.Foreground = WpfTheme.Red;
                if (showErrors)
                    MessageBox.Show(
                        this,
                        exception.Message,
                        AppInfo.ShortName,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
            }
        }

        private void BrowseTarget(
            object sender,
            RoutedEventArgs eventArgs)
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog =
                new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description =
                    "选择 Renishaw Ballbar 20 根目录或 Application 目录";
                if (Directory.Exists(targetBox.Text))
                    dialog.SelectedPath = targetBox.Text;
                if (dialog.ShowDialog() ==
                    System.Windows.Forms.DialogResult.OK)
                {
                    targetBox.Text = dialog.SelectedPath;
                    RefreshTargetState(true);
                }
            }
        }

        private void RunOperation(Action operation)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                operation();
            }
            catch (Exception exception)
            {
                Logger.Error("操作失败", exception);
                MessageBox.Show(
                    this,
                    "操作失败：" + exception.Message,
                    AppInfo.ShortName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private static string DefaultTarget()
        {
            string programFilesX86 =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(
                programFilesX86, "Renishaw Ballbar 20");
        }

        private static TextBlock CardTitle(string text)
        {
            TextBlock title = WpfUi.Text(
                text, 16, WpfTheme.Text);
            title.FontWeight = FontWeights.Bold;
            return title;
        }

        private static void OpenDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
            Process.Start(
                "explorer.exe", "\"" + directory + "\"");
        }
    }
}
