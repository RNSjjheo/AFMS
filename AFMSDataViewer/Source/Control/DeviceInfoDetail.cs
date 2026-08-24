using AFMSDll;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace AFMSDataViewer
{
    public class DeviceInfoDetail : TableLayoutPanel
    {
        public AFMSLabel uiKey;
        public AFMSLabel uiDesc;
        public AFMSLabel uiValue;

        public DeviceInfoDetail()
        {
            Dock = DockStyle.Fill;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 1;
            ColumnCount = 3;
            Margin = new Padding(0, 3, 10, 3);
            Padding = Padding.Empty;
            this.BackColor = Color.White;

            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiKey = CreateLabel();
            uiDesc = CreateLabel();
            uiValue = CreateLabel();
            uiValue.BackColor = DllColorHelper.HexToColor("#E9F4FF");
            uiValue.BorderThickness = 1;
            uiValue.BorderRadius = 5;
            uiValue.BorderColor = DllColorHelper.HexToColor("#C3D8FB");
            uiValue.Font = new Font("Segoe UI", 8, FontStyle.Regular, GraphicsUnit.Point);
            uiValue.TextAlign = ContentAlignment.MiddleCenter;
            uiValue.ForeColor = DllColorHelper.HexToColor("#205EEA");

            Controls.Add(uiKey, 0, 0);
            Controls.Add(uiDesc, 1, 0);
            Controls.Add(uiValue, 2, 0);
        }

        private AFMSLabel CreateLabel()
        {
            AFMSLabel label = new AFMSLabel();
            label.Dock = DockStyle.Fill;
            label.BackColor = Color.White;
            label.BorderThickness = 0;
            label.BorderRadius = 0;
            label.BorderStyle = BorderStyle.None;
            label.Margin = Padding.Empty;
            label.Padding = Padding.Empty;
            label.CharacterSpacing = -0.5F;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 8, FontStyle.Regular, GraphicsUnit.Point);
            label.ForeColor = DllColorHelper.GetDescStrColor();

            return label;
        }
    }
}
