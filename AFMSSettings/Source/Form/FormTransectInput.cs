using AFMSDll;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormTransectInput : AFMSForm
    {
        private readonly int _hydroId;
        private readonly int _transectCount;
        private readonly List<AFMSNumberBox> _distanceInputs = new List<AFMSNumberBox>();
        private readonly AFMSButton uiBtnSave;

        public FormTransectInput(int hydroId, int transectCount)
        {
            if (hydroId <= 0) throw new ArgumentOutOfRangeException(nameof(hydroId));
            if (transectCount <= 0) throw new ArgumentOutOfRangeException(nameof(transectCount));

            _hydroId = hydroId;
            _transectCount = transectCount;

            Text = "측선 입력";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, Math.Min(720, Math.Max(360, 190 + (transectCount * 48))));
            MinimumSize = new Size(520, 360);
            MaximumSize = new Size(520, 720);
            BorderRadius = 8;
            ShowInfoButton = false;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ContentBackColor = Color.White;

            AFMSPanel uiPanelMain = new AFMSPanel();
            uiPanelMain.Dock = DockStyle.Fill;
            uiPanelMain.BackColor = DllColorHelper.HexToColor("#FAFCFB");
            uiPanelMain.BorderRadius = 8;
            uiPanelMain.Padding = new Padding(14, 12, 14, 14);
            uiPanelMain.Margin = Padding.Empty;

            TableLayoutPanel uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowCount = 4;
            uiTpMain.Padding = Padding.Empty;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));

            Label uiLbDesc = new Label();
            uiLbDesc.Dock = DockStyle.Fill;
            uiLbDesc.AutoSize = false;
            uiLbDesc.Text = $"좌안 기준으로 {_transectCount}개 측선의 거리를 입력하세요.";
            uiLbDesc.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F, FontStyle.Regular);
            uiLbDesc.ForeColor = DllColorHelper.GetDescStrColor();
            uiLbDesc.TextAlign = ContentAlignment.MiddleLeft;
            uiLbDesc.Margin = Padding.Empty;

            TableLayoutPanel uiTpHeader = CreateHeader();

            Panel uiPnScroll = new Panel();
            uiPnScroll.Dock = DockStyle.Fill;
            uiPnScroll.AutoScroll = true;
            uiPnScroll.BackColor = Color.White;
            uiPnScroll.Margin = new Padding(0, 0, 0, 10);
            uiPnScroll.Padding = Padding.Empty;

            TableLayoutPanel uiTpInputs = CreateInputRows();
            uiPnScroll.Controls.Add(uiTpInputs);

            uiBtnSave = new AFMSButton();
            uiBtnSave.Dock = DockStyle.Fill;
            uiBtnSave.BorderRadius = 4;
            uiBtnSave.Text = "저장";
            uiBtnSave.BackColor = DllColorHelper.HexToColor("#02925D");
            uiBtnSave.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiBtnSave.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiBtnSave.ForeColor = Color.White;
            uiBtnSave.BorderThickness = 0F;
            uiBtnSave.Margin = Padding.Empty;
            uiBtnSave.Click += UiBtnSave_Click;

            uiTpMain.Controls.Add(uiLbDesc, 0, 0);
            uiTpMain.Controls.Add(uiTpHeader, 0, 1);
            uiTpMain.Controls.Add(uiPnScroll, 0, 2);
            uiTpMain.Controls.Add(uiBtnSave, 0, 3);
            uiPanelMain.Controls.Add(uiTpMain);
            Controls.Add(uiPanelMain);
        }

        public int HydroId => _hydroId;
        public int TransectCount => _transectCount;
        public IReadOnlyList<double> DistanceDatas => GetDistanceDatas();

        private TableLayoutPanel CreateHeader()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 1;
            panel.Margin = Padding.Empty;
            panel.Padding = Padding.Empty;
            panel.BackColor = DllColorHelper.HexToColor("#F5F8F6");
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            panel.Controls.Add(CreateHeaderLabel("측선 번호"), 0, 0);
            panel.Controls.Add(CreateHeaderLabel("좌안에서의 거리 (m)"), 1, 0);
            return panel;
        }

        private TableLayoutPanel CreateInputRows()
        {
            TableLayoutPanel panel = new TableLayoutPanel();
            panel.AutoSize = true;
            panel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            panel.Dock = DockStyle.Top;
            panel.ColumnCount = 2;
            panel.RowCount = _transectCount;
            panel.Margin = Padding.Empty;
            panel.Padding = Padding.Empty;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < _transectCount; i++)
            {
                panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

                Label label = new Label();
                label.Dock = DockStyle.Fill;
                label.AutoSize = false;
                label.Text = $"{i + 1}";
                label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F, FontStyle.Regular);
                label.ForeColor = DllColorHelper.HexToColor("#424A52");
                label.TextAlign = ContentAlignment.MiddleCenter;
                label.Margin = new Padding(0, 2, 8, 2);

                AFMSNumberBox input = new AFMSNumberBox();
                input.Dock = DockStyle.Fill;
                input.InputType = AFMSNumericInputType.Double;
                input.AllowNegative = false;
                input.BorderRadius = 4;
                input.TextAlign = HorizontalAlignment.Center;
                input.Margin = new Padding(8, 6, 8, 6);

                _distanceInputs.Add(input);
                panel.Controls.Add(label, 0, i);
                panel.Controls.Add(input, 1, i);
            }

            return panel;
        }

        private Label CreateHeaderLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Text = text;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10F, FontStyle.Bold);
            label.ForeColor = DllColorHelper.HexToColor("#244B37");
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Margin = Padding.Empty;
            return label;
        }

        private void UiBtnSave_Click(object? sender, EventArgs e)
        {
            if (!TryGetDistanceDatas(out List<double> distances, out int emptyIndex))
            {
                MessageBox.Show($"{emptyIndex + 1}번 측선의 좌안 기준 거리를 입력하세요.", "측선 입력", MessageBoxButtons.OK, MessageBoxIcon.Information);
                _distanceInputs[emptyIndex].Focus();
                return;
            }

            string error = SaveTransect(distances);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "측선 정보 저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private bool TryGetDistanceDatas(out List<double> distances, out int emptyIndex)
        {
            distances = new List<double>(_transectCount);
            emptyIndex = -1;

            for (int i = 0; i < _distanceInputs.Count; i++)
            {
                double? value = _distanceInputs[i].DoubleValue;
                if (!value.HasValue)
                {
                    emptyIndex = i;
                    return false;
                }

                distances.Add(value.Value);
            }

            return distances.Count == _transectCount;
        }

        private IReadOnlyList<double> GetDistanceDatas()
        {
            List<double> result = new List<double>(_distanceInputs.Count);
            foreach (AFMSNumberBox input in _distanceInputs) if (input.DoubleValue.HasValue) result.Add(input.DoubleValue.Value);
            return result;
        }

        private string SaveTransect(List<double> distances)
        {
            DateTime now = DateTime.Now;
            List<object> transects = new List<object>(distances.Count);
            for (int i = 0; i < distances.Count; i++) transects.Add(new { no = i + 1, distance = distances[i] });
            string json = JsonSerializer.Serialize(transects);

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.AutoIncrement = FbtAFMSHydroTransect.COL_ID;
            query.Value(FbtAFMSHydroTransect.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSHydroTransect.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSHydroTransect.COL_HYDRO_ID, _hydroId);
            query.Value(FbtAFMSHydroTransect.COL_TRANSECT_COUNT, _transectCount);
            query.Value(FbtAFMSHydroTransect.COL_DISTANCE_DATAS, json);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);
            return error;
        }
    }
}