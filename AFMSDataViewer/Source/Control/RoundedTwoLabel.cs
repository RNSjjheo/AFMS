using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSDataViewer
{
    [ToolboxItem(true)]
    [DefaultProperty(nameof(BackColor))]
    public class RoundedTwoLabel:AFMSPanel
    {
        public TableLayoutPanel MainTablePanel = new TableLayoutPanel();
        public Label LbKey = new Label();
        public Label LbValue = new Label();
        public RoundedTwoLabel(bool isValueTop)
        {
            this.BorderRadius = 4;
            this.Padding = new Padding(4);
            MainTablePanel.Dock = DockStyle.Fill;
            MainTablePanel.RowStyles.Clear();
            MainTablePanel.ColumnStyles.Clear();
            MainTablePanel.RowCount = 2;
            MainTablePanel.ColumnCount = 1;

            MainTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            MainTablePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            MainTablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            SetupLabel(LbKey);
            SetupLabel(LbValue);

            LbKey.ForeColor = ColorTranslator.FromHtml("#64748B");
            LbKey.Font = new("맑은 고딕", 8.0F, FontStyle.Bold);

            if (isValueTop)
            {
                MainTablePanel.Controls.Add(LbValue, 0, 0);
                MainTablePanel.Controls.Add(LbKey, 0, 1);
            }
            else
            {
                MainTablePanel.Controls.Add(LbKey, 0, 0);
                MainTablePanel.Controls.Add(LbValue, 0, 1);
            }

            this.Controls.Add(MainTablePanel);
        }

        private void SetupLabel(Label label)
        {
            label.AutoSize = false;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.BackColor = Color.Transparent;
            label.Margin = Padding.Empty;
            label.Padding = Padding.Empty;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Value
        {
            get => LbValue.Text;
            set => LbValue.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Key
        {
            get => LbKey.Text;
            set => LbKey.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font ValueFont
        {
            get => LbValue.Font;
            set => LbValue.Font = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Font KeyFont
        {
            get => LbKey.Font;
            set => LbKey.Font = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public  Color ValueForeColor
        {
            get => LbValue.ForeColor;
            set => LbValue.ForeColor = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color KeyForeColor
        {
            get => LbKey.ForeColor;
            set => LbKey.ForeColor = value;
        }
    }
}
