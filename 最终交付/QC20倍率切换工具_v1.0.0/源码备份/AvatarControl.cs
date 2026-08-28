using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Wataa.QC20Tool
{
    internal sealed class AvatarControl : Control
    {
        private static Image cachedAvatar;

        public AvatarControl()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
            Size = new Size(64, 64);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = new Rectangle(
                2, 2, Width - 5, Height - 5);
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddEllipse(bounds);
                GraphicsState state = e.Graphics.Save();
                e.Graphics.SetClip(path);
                Image avatar = GetAvatar();
                DrawCover(e.Graphics, avatar, bounds);
                e.Graphics.Restore(state);
            }
            using (Pen border = new Pen(
                Theme.IsDark
                    ? Color.FromArgb(110, 255, 255, 255)
                    : Color.FromArgb(210, 255, 255, 255),
                3f))
                e.Graphics.DrawEllipse(border, bounds);
        }

        private static Image GetAvatar()
        {
            if (cachedAvatar != null)
                return cachedAvatar;
            byte[] bytes = EncryptedResourceLoader.Read("avatar.enc");
            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                using (Image loaded = Image.FromStream(stream))
                    cachedAvatar = new Bitmap(loaded);
                return cachedAvatar;
            }
            finally
            {
                Array.Clear(bytes, 0, bytes.Length);
            }
        }

        private static void DrawCover(
            Graphics graphics,
            Image image,
            Rectangle bounds)
        {
            double sourceRatio =
                (double)image.Width / image.Height;
            double targetRatio =
                (double)bounds.Width / bounds.Height;
            Rectangle source;
            if (sourceRatio > targetRatio)
            {
                int width = (int)(image.Height * targetRatio);
                source = new Rectangle(
                    (image.Width - width) / 2,
                    0,
                    width,
                    image.Height);
            }
            else
            {
                int height = (int)(image.Width / targetRatio);
                source = new Rectangle(
                    0,
                    (image.Height - height) / 2,
                    image.Width,
                    height);
            }
            graphics.DrawImage(
                image,
                bounds,
                source,
                GraphicsUnit.Pixel);
        }
    }
}
