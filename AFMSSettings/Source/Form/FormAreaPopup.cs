using AFMSDll;
using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSSettings
{
    public class FormAreaPopup:AFMSForm
    {
        private readonly AFMSAreaChart _chart;
        private readonly AreaPointDatas _data;

        private readonly AFMSNumberBox _uiWaterLevel;
        private readonly AFMSLabel _uiArea;

        private readonly TableLayoutPanel _uiTpMain;
        public FormAreaPopup(AFMSAreaChart chart, AreaPointDatas data)
        {
            _chart = chart;
            _data = data;

            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;
            ShowIcon = false;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;

            MinimumSize = new Size(210, 120);
            MaximumSize = new Size(210, 120);

            BackColor = Color.White;
            Text = "단면적";

            _uiTpMain = new TableLayoutPanel();
            _uiTpMain.Dock = DockStyle.Fill;
            _uiTpMain.RowStyles.Clear();
            _uiTpMain.ColumnStyles.Clear();
            _uiTpMain.RowCount = 2;
            _uiTpMain.ColumnCount = 2;
            _uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            _uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            _uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            _uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            AFMSLabel waterLabel = new AFMSLabel();
            waterLabel.Dock = DockStyle.Fill;
            waterLabel.TextAlign = ContentAlignment.MiddleCenter;
            waterLabel.Text = "수위";

            _uiWaterLevel = new AFMSNumberBox();
            _uiWaterLevel.Dock = DockStyle.Fill;
            _uiWaterLevel.AllowNegative = true;
            _uiWaterLevel.InputType = AFMSNumericInputType.Double;
            _uiWaterLevel.Hint = "수위(m)를 입력하세요.";

            AFMSLabel areaLabel = new AFMSLabel();
            areaLabel.Dock = DockStyle.Fill;
            areaLabel.TextAlign = ContentAlignment.MiddleCenter;
            areaLabel.Text = "단면적";

            _uiArea = new AFMSLabel();
            _uiArea.Dock = DockStyle.Fill;
            _uiArea.TextAlign = ContentAlignment.MiddleRight;
            _uiArea.Text = "단면적";

            _uiTpMain.Controls.Add(waterLabel, 0, 0);
            _uiTpMain.Controls.Add(_uiWaterLevel, 1, 0);
            _uiTpMain.Controls.Add(areaLabel, 0, 1);
            _uiTpMain.Controls.Add(_uiArea, 1, 1);

            Controls.Add(_uiTpMain);

            _uiWaterLevel.TextChanged += WaterLevel_TextChanged;

            if (_data.WaterLevel.HasValue)
            {
                _uiWaterLevel.SetValue(_data.WaterLevel.Value);
                UpdateArea();
            }
        }

        private void WaterLevel_TextChanged(object? sender, EventArgs e)
        {
            if (!_uiWaterLevel.TryGetDouble(out double waterLevel))
            {
                _data.WaterLevel = null;
                _uiArea.Text = "0 m²";
                _chart.Invalidate();
                return;
            }

            _data.WaterLevel = waterLevel;

            UpdateArea();

            _chart.Invalidate();
        }

        private void UpdateArea()
        {
            double area = _data.Area;

            _uiArea.Text = $"{area:N2} m²";
        }

        protected override bool ShowWithoutActivation => false;
    }
}
