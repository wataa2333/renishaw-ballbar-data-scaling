using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Wataa.QC20Tool
{
    internal sealed class MainForm : Form
    {
        private Panel content;
        private Panel directoryPanel;
        private Panel factorPanel;
        private Panel actionPanel;
        private TextBox targetBox;
        private Label statusLabel;
        private Label selectedLabel;
        private Label explanationLabel;
        private NumericUpDown manualPercent;
        private Panel manualMode;
        private FlowLayoutPanel presetMode;
        private AccentButton manualTab;
        private AccentButton presetTab;
        private List<AccentButton> presetButtons;
        private bool usingManual;
        private double selectedPercent = 50.0;

        private static readonly int[] Presets =
        {
            1, 5, 10, 20, 25, 30, 40, 50, 60,
            75, 80, 90, 200, 250, 300, 400, 500
        };

        public MainForm(string initialTarget)
        {
            Text = AppInfo.ProductName;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1080, 760);
            MinimumSize = new Size(920, 690);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font(
                "Segoe UI",
                9.5f,
                FontStyle.Regular,
                GraphicsUnit.Point);

            Panel sidebar = BuildSidebar();
            Controls.Add(sidebar);

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.Transparent;
            content.AutoScroll = true;
            Controls.Add(content);
            content.BringToFront();

            BuildHeader();
            directoryPanel = BuildDirectoryPanel(initialTarget);
            factorPanel = BuildFactorPanel();
            actionPanel = BuildActionPanel();
            content.Controls.Add(directoryPanel);
            content.Controls.Add(factorPanel);
            content.Controls.Add(actionPanel);
            BuildFooter();

            content.Resize += delegate { ArrangeContent(); };
            Shown += delegate
            {
                DwmHelper.Apply(this);
                ArrangeContent();
                RefreshTargetState(false);
            };
        }

        private Panel BuildSidebar()
        {
            Panel sidebar = new Panel();
            sidebar.Dock = DockStyle.Left;
            sidebar.Width = 214;
            sidebar.BackColor = Theme.SurfaceAlt;
            sidebar.Padding = new Padding(14, 22, 14, 16);

            AvatarControl avatar = new AvatarControl();
            avatar.Size = new Size(62, 62);
            avatar.Location = new Point(18, 20);
            sidebar.Controls.Add(avatar);

            Label user = NewLabel(
                "wataa",
                new Font("Segoe UI", 12f),
                new Point(90, 30),
                new Size(104, 25));
            sidebar.Controls.Add(user);
            Label verified = NewLabel(
                "动态口令已验证",
                new Font("Segoe UI", 8.7f),
                new Point(90, 55),
                new Size(110, 22));
            verified.ForeColor = Theme.Green;
            sidebar.Controls.Add(verified);

            AccentButton switchButton = NavButton(
                "倍率切换", new Point(14, 112));
            switchButton.Kind = AccentKind.Primary;
            sidebar.Controls.Add(switchButton);

            AccentButton backupButton = NavButton(
                "查看原版备份", new Point(14, 162));
            backupButton.Click += delegate
            {
                OpenDirectory(AppPaths.Backups);
            };
            sidebar.Controls.Add(backupButton);

            AccentButton logButton = NavButton(
                "打开操作日志", new Point(14, 212));
            logButton.Click += delegate
            {
                OpenDirectory(AppPaths.Logs);
            };
            sidebar.Controls.Add(logButton);

            Label version = NewLabel(
                "v" + AppInfo.Version + Environment.NewLine +
                "制作人：" + AppInfo.Author + Environment.NewLine +
                AppInfo.Email,
                new Font("Segoe UI", 8.5f),
                new Point(18, 600),
                new Size(178, 70));
            version.Anchor = AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Bottom;
            version.ForeColor = Theme.Muted;
            sidebar.Controls.Add(version);
            return sidebar;
        }

        private void BuildHeader()
        {
            Label heading = NewLabel(
                "位移偏差数据倍率",
                new Font("Segoe UI", 20f, FontStyle.Regular),
                new Point(30, 24),
                new Size(360, 40));
            content.Controls.Add(heading);

            statusLabel = NewLabel(
                "等待检测目录",
                new Font("Segoe UI", 9f),
                new Point(560, 30),
                new Size(250, 32));
            statusLabel.TextAlign = ContentAlignment.MiddleRight;
            statusLabel.ForeColor = Theme.Muted;
            content.Controls.Add(statusLabel);
        }

        private Panel BuildDirectoryPanel(string initialTarget)
        {
            Panel panel = NewSurfacePanel(new Point(30, 80), 130);
            panel.Controls.Add(SectionTitle(
                "Renishaw Ballbar 20 软件位置", 18, 15));

            targetBox = new TextBox();
            targetBox.Location = new Point(18, 50);
            targetBox.Size = new Size(610, 30);
            targetBox.Anchor = AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Top;
            targetBox.BackColor = Theme.SurfaceAlt;
            targetBox.ForeColor = Theme.Text;
            targetBox.BorderStyle = BorderStyle.FixedSingle;
            targetBox.Text = string.IsNullOrEmpty(initialTarget)
                ? DefaultTarget()
                : initialTarget;
            panel.Controls.Add(targetBox);

            AccentButton browse = new AccentButton();
            browse.Text = "浏览…";
            browse.Location = new Point(640, 47);
            browse.Size = new Size(100, 36);
            browse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            browse.Click += BrowseTarget;
            panel.Controls.Add(browse);

            AccentButton detect = new AccentButton();
            detect.Text = "检测目录";
            detect.Location = new Point(748, 47);
            detect.Size = new Size(105, 36);
            detect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            detect.Click += delegate { RefreshTargetState(true); };
            panel.Controls.Add(detect);

            Label hint = NewLabel(
                "可选择软件根目录或 Application 目录；工具会核对两份 DLL。",
                new Font("Segoe UI", 8.7f),
                new Point(18, 90),
                new Size(720, 24));
            hint.ForeColor = Theme.Muted;
            panel.Controls.Add(hint);
            return panel;
        }

        private Panel BuildFactorPanel()
        {
            Panel panel = NewSurfacePanel(new Point(30, 224), 300);
            panel.Controls.Add(SectionTitle("选择目标倍率", 18, 14));

            manualTab = new AccentButton();
            manualTab.Text = "手动输入";
            manualTab.Location = new Point(18, 48);
            manualTab.Size = new Size(120, 36);
            manualTab.Click += delegate { SetMode(true); };
            panel.Controls.Add(manualTab);

            presetTab = new AccentButton();
            presetTab.Text = "预设列表";
            presetTab.Kind = AccentKind.Primary;
            presetTab.Location = new Point(145, 48);
            presetTab.Size = new Size(120, 36);
            presetTab.Click += delegate { SetMode(false); };
            panel.Controls.Add(presetTab);

            manualMode = new Panel();
            manualMode.Location = new Point(18, 98);
            manualMode.Size = new Size(835, 110);
            manualMode.Anchor = AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Top;
            manualMode.Visible = false;
            panel.Controls.Add(manualMode);

            Label manualLabel = NewLabel(
                "输入百分比",
                Font,
                new Point(0, 5),
                new Size(100, 26));
            manualMode.Controls.Add(manualLabel);
            manualPercent = new NumericUpDown();
            manualPercent.Location = new Point(0, 36);
            manualPercent.Size = new Size(185, 32);
            manualPercent.Minimum = 1;
            manualPercent.Maximum = 500;
            manualPercent.DecimalPlaces = 0;
            manualPercent.Value = 50;
            manualPercent.BackColor = Theme.SurfaceAlt;
            manualPercent.ForeColor = Theme.Text;
            manualPercent.ValueChanged += delegate
            {
                selectedPercent =
                    Convert.ToDouble(manualPercent.Value);
                UpdateSelection();
            };
            manualMode.Controls.Add(manualPercent);
            Label range = NewLabel(
                "%    允许范围 1%–500%",
                new Font("Segoe UI", 9f),
                new Point(195, 39),
                new Size(250, 28));
            range.ForeColor = Theme.Muted;
            manualMode.Controls.Add(range);

            presetMode = new FlowLayoutPanel();
            presetMode.Location = new Point(18, 98);
            presetMode.Size = new Size(835, 115);
            presetMode.Anchor = AnchorStyles.Left |
                AnchorStyles.Right | AnchorStyles.Top;
            presetMode.AutoScroll = false;
            presetMode.WrapContents = true;
            presetMode.BackColor = Color.Transparent;
            panel.Controls.Add(presetMode);

            presetButtons = new List<AccentButton>();
            foreach (int preset in Presets)
            {
                AccentButton button = new AccentButton();
                button.Text = preset + "%";
                button.Size = new Size(82, 38);
                button.Margin = new Padding(0, 0, 8, 8);
                button.Tag = preset;
                if (preset == 50)
                    button.Kind = AccentKind.Primary;
                button.Click += PresetClick;
                presetButtons.Add(button);
                presetMode.Controls.Add(button);
            }

            selectedLabel = NewLabel(
                "当前选择：50%（×0.50）",
                new Font("Segoe UI", 11f),
                new Point(18, 224),
                new Size(360, 30));
            panel.Controls.Add(selectedLabel);
            explanationLabel = NewLabel(
                "位移偏差数据缩小为原值的 50%",
                new Font("Segoe UI", 9f),
                new Point(390, 226),
                new Size(463, 28));
            explanationLabel.TextAlign =
                ContentAlignment.MiddleRight;
            explanationLabel.Anchor =
                AnchorStyles.Top | AnchorStyles.Right;
            explanationLabel.ForeColor = Theme.Muted;
            panel.Controls.Add(explanationLabel);

            Label warning = NewLabel(
                "仅缩放位移偏差数据源；角度、温度、半径和日期不按倍率改变。",
                new Font("Segoe UI", 8.7f),
                new Point(18, 264),
                new Size(720, 24));
            warning.ForeColor = Theme.Muted;
            panel.Controls.Add(warning);
            return panel;
        }

        private Panel BuildActionPanel()
        {
            Panel panel = NewSurfacePanel(new Point(30, 538), 96);

            AccentButton package = new AccentButton();
            package.Text = "生成手动替换包";
            package.Kind = AccentKind.Positive;
            package.Location = new Point(18, 25);
            package.Size = new Size(175, 44);
            package.Click += GenerateManualPackage;
            panel.Controls.Add(package);

            AccentButton restore = new AccentButton();
            restore.Text = "恢复原版";
            restore.Kind = AccentKind.Danger;
            restore.Location = new Point(573, 25);
            restore.Size = new Size(130, 44);
            restore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            restore.Click += RestoreOriginal;
            panel.Controls.Add(restore);

            AccentButton apply = new AccentButton();
            apply.Text = "应用所选倍率";
            apply.Kind = AccentKind.Primary;
            apply.Location = new Point(713, 25);
            apply.Size = new Size(140, 44);
            apply.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            apply.Click += ApplyFactor;
            panel.Controls.Add(apply);
            return panel;
        }

        private void BuildFooter()
        {
            Label footer = NewLabel(
                "仅限学习研究测试，不用于正式计量或验收" +
                Environment.NewLine +
                "制作人：wataa  ·  邮箱：" + AppInfo.Email,
                new Font("Segoe UI", 8.7f),
                new Point(30, 651),
                new Size(860, 48));
            footer.ForeColor = Theme.Muted;
            footer.TextAlign = ContentAlignment.TopRight;
            footer.Anchor = AnchorStyles.Top |
                AnchorStyles.Left | AnchorStyles.Right;
            footer.Name = "FooterLabel";
            content.Controls.Add(footer);
        }

        private void SetMode(bool manual)
        {
            usingManual = manual;
            manualMode.Visible = manual;
            presetMode.Visible = !manual;
            manualTab.Kind = manual
                ? AccentKind.Primary
                : AccentKind.Neutral;
            presetTab.Kind = manual
                ? AccentKind.Neutral
                : AccentKind.Primary;
            if (manual)
                selectedPercent =
                    Convert.ToDouble(manualPercent.Value);
            UpdateSelection();
        }

        private void PresetClick(object sender, EventArgs eventArgs)
        {
            AccentButton clicked = (AccentButton)sender;
            selectedPercent = Convert.ToDouble(clicked.Tag);
            foreach (AccentButton button in presetButtons)
                button.Kind = ReferenceEquals(button, clicked)
                    ? AccentKind.Primary
                    : AccentKind.Neutral;
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            double factor = selectedPercent / 100.0;
            selectedLabel.Text =
                "当前选择：" +
                selectedPercent.ToString(
                    "0.##", CultureInfo.InvariantCulture) +
                "%（×" +
                factor.ToString(
                    "0.00", CultureInfo.InvariantCulture) +
                "）";

            if (selectedPercent < 100)
                explanationLabel.Text =
                    "位移偏差数据缩小为原值的 " +
                    selectedPercent.ToString(
                        "0.##", CultureInfo.InvariantCulture) +
                    "%";
            else if (Math.Abs(selectedPercent - 100) < 0.0001)
                explanationLabel.Text =
                    "位移偏差数据保持原值";
            else
                explanationLabel.Text =
                    "位移偏差数据放大为原值的 " +
                    factor.ToString(
                        "0.##", CultureInfo.InvariantCulture) +
                    " 倍";
        }

        private void ApplyFactor(object sender, EventArgs eventArgs)
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
                        "两份 DLL 已同时处理并通过 SHA-256 校验。",
                        AppInfo.ShortName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                });
        }

        private void RestoreOriginal(
            object sender,
            EventArgs eventArgs)
        {
            if (MessageBox.Show(
                    "将同时恢复两份原版 DLL。是否继续？",
                    "恢复原版",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
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
                        "原版已恢复并通过 SHA-256 校验。",
                        AppInfo.ShortName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                });
        }

        private void GenerateManualPackage(
            object sender,
            EventArgs eventArgs)
        {
            RunOperation(
                delegate
                {
                    string application = ResolveTarget();
                    BackupSet backup =
                        BackupService.EnsureOriginalBackup(application);
                    using (FolderBrowserDialog dialog =
                        new FolderBrowserDialog())
                    {
                        dialog.Description =
                            "选择手动替换包的输出位置";
                        if (dialog.ShowDialog(this) !=
                            DialogResult.OK)
                            return;
                        string output = ManualPackageService.Generate(
                            dialog.SelectedPath,
                            backup,
                            selectedPercent / 100.0);
                        MessageBox.Show(
                            "手动替换包已生成：" +
                            Environment.NewLine + output,
                            AppInfo.ShortName,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
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

            DialogResult answer = MessageBox.Show(
                "当前用户没有目标目录写入权限。" +
                Environment.NewLine +
                "是否以管理员身份重新启动？重新启动后需要再次输入动态口令。",
                "需要管理员权限",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
                return false;
            if (PermissionService.RestartElevated(targetBox.Text))
            {
                Logger.Info("用户同意以管理员身份重新启动");
                BeginInvoke(new MethodInvoker(Application.Exit));
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
            {
                details.AppendLine(
                    process.Name + "  PID " + process.Id);
            }
            DialogResult answer = MessageBox.Show(
                "检测到 Renishaw Ballbar 20 正在运行：" +
                Environment.NewLine + Environment.NewLine +
                details.ToString() + Environment.NewLine +
                "必须结束这些进程后才能安全替换 DLL。" +
                "是否结束进程并继续？",
                "软件正在运行",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (answer != DialogResult.Yes)
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
                statusLabel.Text = "目录有效 · 当前 " + state;
                statusLabel.ForeColor =
                    state == "未知或不匹配"
                    ? Theme.Red
                    : Theme.Green;
            }
            catch (Exception exception)
            {
                statusLabel.Text = "目录无效或尚未选择";
                statusLabel.ForeColor = Theme.Red;
                if (showErrors)
                    MessageBox.Show(
                        exception.Message,
                        AppInfo.ShortName,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }
        }

        private void BrowseTarget(object sender, EventArgs eventArgs)
        {
            using (FolderBrowserDialog dialog =
                new FolderBrowserDialog())
            {
                dialog.Description =
                    "选择 Renishaw Ballbar 20 根目录或 Application 目录";
                if (Directory.Exists(targetBox.Text))
                    dialog.SelectedPath = targetBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
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
                UseWaitCursor = true;
                operation();
            }
            catch (Exception exception)
            {
                Logger.Error("操作失败", exception);
                MessageBox.Show(
                    "操作失败：" + exception.Message,
                    AppInfo.ShortName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
            }
        }

        private void ArrangeContent()
        {
            int width = Math.Max(650, content.ClientSize.Width - 60);
            directoryPanel.Width = width;
            factorPanel.Width = width;
            actionPanel.Width = width;
            statusLabel.Left = Math.Max(420, width - 220);
            Control[] footers =
                content.Controls.Find("FooterLabel", false);
            if (footers.Length > 0)
                footers[0].Width = width;
        }

        private static string DefaultTarget()
        {
            string programFilesX86 =
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ProgramFilesX86);
            return Path.Combine(
                programFilesX86, "Renishaw Ballbar 20");
        }

        private static AccentButton NavButton(
            string text,
            Point location)
        {
            AccentButton button = new AccentButton();
            button.Text = text;
            button.Location = location;
            button.Size = new Size(186, 40);
            button.TextAlign = ContentAlignment.MiddleLeft;
            return button;
        }

        private static Panel NewSurfacePanel(
            Point location,
            int height)
        {
            Panel panel = new Panel();
            panel.Location = location;
            panel.Size = new Size(860, height);
            panel.BackColor = Theme.Surface;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Anchor = AnchorStyles.Top |
                AnchorStyles.Left | AnchorStyles.Right;
            return panel;
        }

        private static Label SectionTitle(
            string text,
            int left,
            int top)
        {
            return NewLabel(
                text,
                new Font("Segoe UI", 11f),
                new Point(left, top),
                new Size(420, 28));
        }

        private static Label NewLabel(
            string text,
            Font font,
            Point location,
            Size size)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = font;
            label.Location = location;
            label.Size = size;
            label.BackColor = Color.Transparent;
            label.ForeColor = Theme.Text;
            return label;
        }

        private static void OpenDirectory(string directory)
        {
            Directory.CreateDirectory(directory);
            Process.Start(new ProcessStartInfo(
                "explorer.exe",
                "\"" + directory + "\"")
            {
                UseShellExecute = true
            });
        }
    }
}
