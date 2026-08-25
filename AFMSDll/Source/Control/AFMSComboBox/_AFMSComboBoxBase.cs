using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDll
{
    public abstract class _AFMSComboBoxBase : Control, IAFMSRoundedControl
    {
        private Color _borderColor = Color.FromArgb(218, 224, 232);
        private int _borderRadius = 14;
        private float _borderThickness = 1F;

        [Category("AFMS Appearance")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("AFMS Appearance")]
        [DefaultValue(14)]
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
