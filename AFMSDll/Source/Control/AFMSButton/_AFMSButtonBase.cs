using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDll
{
    public abstract class _AFMSButtonBase : Button, IAFMSRoundedControl
    {
        private Color _borderColor = Color.FromArgb(205, 211, 220);
        private float _borderThickness = 1F;
        private int _borderRadius = 6;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public virtual float BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0F, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public virtual int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }
    }
}
