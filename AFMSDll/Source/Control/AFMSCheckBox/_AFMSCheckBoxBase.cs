using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDll
{
    public abstract class _AFMSCheckBoxBase : CheckBox, IAFMSRoundedControl
    {
        private Color _borderColor = Color.FromArgb(214, 220, 226);
        private int _borderRadius = 6;
        private float _borderThickness = 1F;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public virtual Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(6)]
        public virtual int BorderRadius
        {
            get => _borderRadius;
            set { _borderRadius = Math.Max(0, value); Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(1F)]
        public virtual float BorderThickness
        {
            get => _borderThickness;
            set { _borderThickness = Math.Max(0F, value); Invalidate(); }
        }

    }
}
