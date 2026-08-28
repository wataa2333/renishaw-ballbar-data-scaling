using System;
using System.Drawing;
using System.Windows.Forms;

namespace Wataa.QC20Tool
{
    internal enum AccentKind
    {
        Neutral,
        Primary,
        Danger,
        Positive
    }

    internal sealed class AccentButton : Button
    {
        private AccentKind kind;

        public AccentKind Kind
        {
            get { return kind; }
            set
            {
                kind = value;
                ApplyColors(false);
            }
        }

        public AccentButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 1;
            Height = 40;
            Padding = new Padding(10, 0, 10, 0);
            Cursor = Cursors.Hand;
            Font = new Font(
                "Segoe UI",
                9.5f,
                FontStyle.Regular,
                GraphicsUnit.Point);
            ApplyColors(false);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            ApplyColors(true);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            ApplyColors(false);
        }

        private void ApplyColors(bool hover)
        {
            Color baseColor;
            if (kind == AccentKind.Primary)
                baseColor = Theme.Blue;
            else if (kind == AccentKind.Danger)
                baseColor = Theme.Red;
            else if (kind == AccentKind.Positive)
                baseColor = Theme.Green;
            else
            {
                BackColor = hover
                    ? Theme.SurfaceAlt
                    : Theme.Surface;
                ForeColor = Theme.Text;
                FlatAppearance.BorderColor = Theme.Border;
                return;
            }

            BackColor = hover
                ? ControlPaint.Light(baseColor, 0.08f)
                : baseColor;
            ForeColor = Color.White;
            FlatAppearance.BorderColor =
                ControlPaint.Dark(baseColor, 0.08f);
        }
    }
}
