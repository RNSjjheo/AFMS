using AFMSDll;
using System.ComponentModel;

namespace AFMSLoggerMonitors
{
    internal sealed class AutoScrollButton : Button
    {
        private bool autoScrollEnabled = true;

        public AutoScrollButton()
        {
            Cursor = Cursors.Hand;
            Dock = DockStyle.Fill;
            FlatStyle = FlatStyle.Flat;
            Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 8.5F, FontStyle.Regular, GraphicsUnit.Point);
            Margin = new Padding(4, 1, 0, 1);
            Padding = Padding.Empty;
            Text = "↓ 자동 스크롤";
            UseVisualStyleBackColor = false;
            UpdateAppearance();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool AutoScrollEnabled
        {
            get => autoScrollEnabled;
            set
            {
                if (autoScrollEnabled == value) return;
                autoScrollEnabled = value;
                UpdateAppearance();
            }
        }

        private void UpdateAppearance()
        {
            BackColor = autoScrollEnabled ? DllColorHelper.HexToColor("#EEF7FF") : Color.White;
            ForeColor = autoScrollEnabled ? DllColorHelper.HexToColor("#315D7E") : TabCommon.DescriptionColor;
            FlatAppearance.BorderColor = autoScrollEnabled ? DllColorHelper.HexToColor("#B7D5EA") : TabCommon.BorderColor;
            FlatAppearance.BorderSize = 1;
            FlatAppearance.MouseOverBackColor = DllColorHelper.HexToColor("#E4F1FA");
            FlatAppearance.MouseDownBackColor = DllColorHelper.HexToColor("#D7EAF7");
        }
    }
}
