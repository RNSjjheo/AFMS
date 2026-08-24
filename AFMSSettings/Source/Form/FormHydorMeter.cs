using AFMSDll;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormHydorMeter : AFMSForm
    {
        private readonly AFMSComboBox uiCbHydroType;
        private readonly AFMSComboBox uiCbHydroComm;
        private readonly AFMSNumberBox uiNoTransectCnt;
        private readonly AFMSButton uiBtnAdd;

        public FormHydorMeter()
        {
            Text = "유속계 추가";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(500, 390);
            MinimumSize = Size;
            MaximumSize = Size;
            BorderRadius = 8;
            ShowInfoButton = false;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ContentBackColor = Color.White;

            AFMSPanel uiPanelMain = new AFMSPanel();
            uiPanelMain.Dock = DockStyle.Fill;
            uiPanelMain.BackColor = DllColorHelper.HexToColor("#FAFCFB");
            uiPanelMain.BorderRadius = 8;
            uiPanelMain.Padding = new Padding(14, 10, 14, 14);
            uiPanelMain.Margin = Padding.Empty;

            TableLayoutPanel uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnCount = 1;
            uiTpMain.RowCount = 8;
            uiTpMain.Padding = Padding.Empty;
            uiTpMain.Margin = Padding.Empty;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));

            Label lbHydroType = CreateLabel("유속계 타입");
            Label lbCommInfo = CreateLabel("연결 정보");
            Label lbTransectCnt = CreateLabel("측선수");

            uiCbHydroType = new AFMSComboBox();
            uiCbHydroType.Dock = DockStyle.Fill;
            uiCbHydroType.BorderRadius = 4;
            uiCbHydroType.Margin = new Padding(0, 0, 0, 6);
            uiCbHydroType.Add(HydroMeterType.RnDVideoCollector);
            uiCbHydroType.Add(HydroMeterType.RnDMpdsCollector);
            uiCbHydroType.SelectedIndex = -1;
            uiCbHydroType.SelectedIndexChanged += UiCbHydroType_SelectedIndexChanged;

            uiCbHydroComm = new AFMSComboBox();
            uiCbHydroComm.Dock = DockStyle.Fill;
            uiCbHydroComm.BorderRadius = 4;
            uiCbHydroComm.Margin = new Padding(0, 0, 0, 6);

            uiNoTransectCnt = new AFMSNumberBox();
            uiNoTransectCnt.Dock = DockStyle.Fill;
            uiNoTransectCnt.BorderRadius = 4;
            uiNoTransectCnt.InputType = AFMSNumericInputType.Integer;
            uiNoTransectCnt.TextAlign = HorizontalAlignment.Center;
            uiNoTransectCnt.Margin = new Padding(0, 0, 0, 6);
            uiNoTransectCnt.SetValue(1);

            uiBtnAdd = new AFMSButton();
            uiBtnAdd.Dock = DockStyle.Fill;
            uiBtnAdd.BorderRadius = 4;
            uiBtnAdd.Text = "추가하기";
            uiBtnAdd.BackColor = DllColorHelper.HexToColor("#02925D");
            uiBtnAdd.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiBtnAdd.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiBtnAdd.ForeColor = Color.White;
            uiBtnAdd.BorderThickness = 0F;
            uiBtnAdd.Margin = Padding.Empty;
            uiBtnAdd.Click += UiBtnAdd_Click;

            uiTpMain.Controls.Add(lbHydroType, 0, 0);
            uiTpMain.Controls.Add(uiCbHydroType, 0, 1);
            uiTpMain.Controls.Add(lbCommInfo, 0, 2);
            uiTpMain.Controls.Add(uiCbHydroComm, 0, 3);
            uiTpMain.Controls.Add(lbTransectCnt, 0, 4);
            uiTpMain.Controls.Add(uiNoTransectCnt, 0, 5);
            uiTpMain.Controls.Add(uiBtnAdd, 0, 7);

            uiPanelMain.Controls.Add(uiTpMain);
            Controls.Add(uiPanelMain);
        }

        public HydroMeterType HydroType => uiCbHydroType.SelectedItem is HydroMeterType type ? type : HydroMeterType.None;
        public string CommConfig => uiCbHydroComm.SelectedItem?.ToString() ?? "";
        public int TransectCount => uiNoTransectCnt.IntValue ?? 1;

        private Label CreateLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.AutoSize = false;
            label.Text = text;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 10.5F, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Margin = new Padding(0, 4, 0, 4);
            return label;
        }

        private void UiCbHydroType_SelectedIndexChanged(object? sender, EventArgs e)
        {
            uiCbHydroComm.ClearItems();
            if (uiCbHydroType.SelectedItem is not HydroMeterType type) return;

            if (type == HydroMeterType.RnDVideoCollector)
            {
                uiCbHydroComm.Add("WEB");
                return;
            }

            for (int i = 1; i <= 100; i++) uiCbHydroComm.Add($"COM{i}");
        }

        private void UiBtnAdd_Click(object? sender, EventArgs e)
        {
            if (uiCbHydroType.SelectedItem is not HydroMeterType)
            {
                MessageBox.Show("유속계 타입을 선택하세요.", "유속계 추가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (uiCbHydroComm.SelectedItem == null)
            {
                MessageBox.Show("연결 정보를 선택하세요.", "유속계 추가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if ((uiNoTransectCnt.IntValue ?? 0) < 1)
            {
                MessageBox.Show("측선수는 1 이상으로 입력하세요.", "유속계 추가", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
