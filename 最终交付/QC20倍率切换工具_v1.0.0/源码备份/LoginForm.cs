using System;
using System.Drawing;
using System.Windows.Forms;

namespace Wataa.QC20Tool
{
    internal sealed class LoginForm : Form
    {
        private readonly TextBox codeBox;
        private readonly Label errorLabel;
        private readonly AccentButton verifyButton;
        private readonly Timer lockTimer;
        private int failedAttempts;
        private DateTime lockedUntil;

        public LoginForm()
        {
            Text = AppInfo.ProductName;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(500, 555);
            MinimumSize = new Size(500, 555);
            MaximumSize = new Size(500, 555);
            BackColor = Theme.Window;
            ForeColor = Theme.Text;
            Font = new Font(
                "Segoe UI",
                9.5f,
                FontStyle.Regular,
                GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            Panel card = new Panel();
            card.BackColor = Theme.Surface;
            card.Location = new Point(58, 36);
            card.Size = new Size(384, 470);
            Controls.Add(card);

            AvatarControl avatar = new AvatarControl();
            avatar.Size = new Size(92, 92);
            avatar.Location = new Point(146, 28);
            card.Controls.Add(avatar);

            Label user = NewLabel(
                "wataa",
                new Font("Segoe UI", 13f, FontStyle.Regular),
                new Point(20, 127),
                new Size(344, 28),
                ContentAlignment.MiddleCenter);
            card.Controls.Add(user);

            Label heading = NewLabel(
                "验证动态口令",
                new Font("Segoe UI", 17f, FontStyle.Regular),
                new Point(20, 174),
                new Size(344, 38),
                ContentAlignment.MiddleCenter);
            card.Controls.Add(heading);

            Label copy = NewLabel(
                "请输入 Google Authenticator 当前显示的 6 位动态口令",
                Font,
                new Point(20, 216),
                new Size(344, 25),
                ContentAlignment.MiddleCenter);
            copy.ForeColor = Theme.Muted;
            card.Controls.Add(copy);

            codeBox = new TextBox();
            codeBox.Location = new Point(68, 257);
            codeBox.Size = new Size(248, 36);
            codeBox.Font = new Font(
                "Consolas",
                18f,
                FontStyle.Regular,
                GraphicsUnit.Point);
            codeBox.TextAlign = HorizontalAlignment.Center;
            codeBox.MaxLength = 6;
            codeBox.UseSystemPasswordChar = true;
            codeBox.BackColor = Theme.SurfaceAlt;
            codeBox.ForeColor = Theme.Text;
            codeBox.BorderStyle = BorderStyle.FixedSingle;
            codeBox.KeyPress += CodeBoxKeyPress;
            card.Controls.Add(codeBox);

            Label meta = NewLabel(
                "30 秒更新    ·    支持离线验证    ·    允许 ±30 秒时间偏差",
                new Font("Segoe UI", 8.8f),
                new Point(20, 307),
                new Size(344, 24),
                ContentAlignment.MiddleCenter);
            meta.ForeColor = Theme.Muted;
            card.Controls.Add(meta);

            verifyButton = new AccentButton();
            verifyButton.Kind = AccentKind.Primary;
            verifyButton.Text = "验证并进入工具";
            verifyButton.Location = new Point(68, 346);
            verifyButton.Size = new Size(248, 44);
            verifyButton.Click += VerifyClick;
            card.Controls.Add(verifyButton);
            AcceptButton = verifyButton;

            errorLabel = NewLabel(
                string.Empty,
                new Font("Segoe UI", 8.8f),
                new Point(20, 399),
                new Size(344, 42),
                ContentAlignment.TopCenter);
            errorLabel.ForeColor = Theme.Red;
            card.Controls.Add(errorLabel);

            Label footer = NewLabel(
                "仅限学习研究测试  ·  制作人：wataa",
                new Font("Segoe UI", 8.8f),
                new Point(20, 444),
                new Size(344, 20),
                ContentAlignment.MiddleCenter);
            footer.ForeColor = Theme.Muted;
            card.Controls.Add(footer);

            lockTimer = new Timer();
            lockTimer.Interval = 1000;
            lockTimer.Tick += LockTimerTick;

            Shown += delegate
            {
                DwmHelper.Apply(this);
                codeBox.Focus();
            };
        }

        private void VerifyClick(object sender, EventArgs eventArgs)
        {
            if (DateTime.UtcNow < lockedUntil)
                return;

            if (TotpService.Verify(codeBox.Text))
            {
                Logger.Info("TOTP 动态口令验证成功");
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            failedAttempts++;
            codeBox.Clear();
            if (failedAttempts >= 5)
            {
                lockedUntil = DateTime.UtcNow.AddSeconds(60);
                failedAttempts = 0;
                verifyButton.Enabled = false;
                lockTimer.Start();
                UpdateLockText();
                Logger.Info("TOTP 连续失败 5 次，临时锁定 60 秒");
            }
            else
            {
                errorLabel.Text = "动态口令不正确，还可尝试 " +
                    (5 - failedAttempts) + " 次。请检查手机和电脑时间。";
                Logger.Info("TOTP 动态口令验证失败");
            }
        }

        private void LockTimerTick(object sender, EventArgs eventArgs)
        {
            if (DateTime.UtcNow >= lockedUntil)
            {
                lockTimer.Stop();
                verifyButton.Enabled = true;
                errorLabel.Text = string.Empty;
                codeBox.Focus();
            }
            else
                UpdateLockText();
        }

        private void UpdateLockText()
        {
            int seconds = (int)Math.Ceiling(
                (lockedUntil - DateTime.UtcNow).TotalSeconds);
            errorLabel.Text =
                "尝试次数过多，请等待 " + seconds + " 秒。";
        }

        private static void CodeBoxKeyPress(
            object sender,
            KeyPressEventArgs eventArgs)
        {
            if (!char.IsControl(eventArgs.KeyChar) &&
                !char.IsDigit(eventArgs.KeyChar))
                eventArgs.Handled = true;
        }

        private static Label NewLabel(
            string text,
            Font font,
            Point location,
            Size size,
            ContentAlignment alignment)
        {
            Label label = new Label();
            label.Text = text;
            label.Font = font;
            label.Location = location;
            label.Size = size;
            label.TextAlign = alignment;
            label.BackColor = Color.Transparent;
            label.ForeColor = Theme.Text;
            return label;
        }
    }
}
