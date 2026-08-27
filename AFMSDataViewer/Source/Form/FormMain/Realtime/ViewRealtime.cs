using AFMSDataViewer.Source.Control;
using AFMSDll;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSDataViewer
{
    public class ViewRealtime : TableLayoutPanel
    {
        public AFMSPanel uiPnHeader;
        public MaximizableTableLayoutPanel uiTpField;
        public AFMSButtonGroup uiBtnGroups;
        public AFMSButtonGroup uiBtnTemp;
        public AFMSNavigatorBox uiNavigator;

        private TableLayoutPanel uiTpTop;
        private ChartSelectPanel uiChart1;
        private ChartSelectPanel uiChart2;
        private ChartSelectPanel uiChart3;
        private ChartSelectPanel uiChart4;
        private DateTime selectedDateTime;

        public ViewRealtime()
        {
            Dock = DockStyle.Fill;
            RowStyles.Clear();
            ColumnStyles.Clear();
            RowCount = 2;
            ColumnCount = 1;
            Margin = Padding.Empty;
            Padding = Padding.Empty;

            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
            RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiPnHeader = new AFMSPanel();
            uiPnHeader.Dock = DockStyle.Fill;
            uiPnHeader.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            uiPnHeader.Padding = new Padding(3, 2, 3, 2);
            uiPnHeader.Margin = Padding.Empty;
            uiPnHeader.BorderRadius = 5;
            uiPnHeader.BorderColor = DllColorHelper.GetCommonBorder();
            uiPnHeader.BorderThickness = 2;

            uiTpField = new MaximizableTableLayoutPanel();
            uiTpField.Dock = DockStyle.Fill;
            uiTpField.BackColor = Color.White;
            uiTpField.Margin = new Padding(0, 10, 0, 0);
            uiTpField.Padding = Padding.Empty;

            uiTpTop = new TableLayoutPanel();
            uiTpTop.Dock = DockStyle.Fill;
            uiTpTop.RowStyles.Clear();
            uiTpTop.ColumnStyles.Clear();
            uiTpTop.RowCount = 1;
            uiTpTop.ColumnCount = 4;
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpTop.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            uiTpTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpTop.Margin = Padding.Empty;
            uiTpTop.Padding = Padding.Empty;

            uiBtnGroups = new AFMSButtonGroup();
            uiBtnGroups.Dock = DockStyle.Fill;
            uiBtnGroups.AddButton("일");
            uiBtnGroups.AddButton("주");
            uiBtnGroups.AddButton("월");
            uiBtnGroups.SelectedIndexChanged += (_, _) => UpdateNavigatorText();

            uiBtnTemp = new AFMSButtonGroup();
            uiBtnTemp.Dock = DockStyle.Fill;
            uiBtnTemp.SelectedBackColor = DllColorHelper.HexToColor("#ECFDF5");
            uiBtnTemp.Click += UiBtnTemp_Click;

            const int layoutIconSize = 16;
            AFMSButtonGroupItem x1 = uiBtnTemp.AddButton(
                AFMSIcon.Layout22Off(layoutIconSize, layoutIconSize),
                AFMSIcon.Layout22On(layoutIconSize, layoutIconSize),
                MaximizableTableLayoutType.Layout2_2);
            AFMSButtonGroupItem x2 = uiBtnTemp.AddButton(
                AFMSIcon.Layout21Off(layoutIconSize, layoutIconSize),
                AFMSIcon.Layout21On(layoutIconSize, layoutIconSize),
                MaximizableTableLayoutType.Layout2_1);
            AFMSButtonGroupItem x3 = uiBtnTemp.AddButton(
                AFMSIcon.Layout12Off(layoutIconSize, layoutIconSize),
                AFMSIcon.Layout12On(layoutIconSize, layoutIconSize),
                MaximizableTableLayoutType.Layout1_2);
            AFMSButtonGroupItem x4 = uiBtnTemp.AddButton(
                AFMSIcon.Layout11Off(layoutIconSize, layoutIconSize),
                AFMSIcon.Layout11On(layoutIconSize, layoutIconSize),
                MaximizableTableLayoutType.Layout1_1);

            uiNavigator = new AFMSNavigatorBox();
            uiNavigator.Dock = DockStyle.Fill;
            uiNavigator.ReadOnly = true;
            uiNavigator.LeftButtonClick += (_, _) => MoveSelectedDate(-1);
            uiNavigator.RightButtonClick += (_, _) => MoveSelectedDate(1);
            DateTime now = DateTime.Now;
            selectedDateTime = new DateTime(
                now.Year, now.Month, now.Day, now.Hour, now.Minute / 10 * 10, 0, now.Kind);
            UpdateNavigatorText();

            uiChart1 = CreateChartPanel();
            uiChart2 = CreateChartPanel();
            uiChart3 = CreateChartPanel();
            uiChart4 = CreateChartPanel();

            uiTpTop.Controls.Add(uiBtnGroups, 0, 0);
            uiTpTop.Controls.Add(uiNavigator, 1, 0);
            uiTpTop.Controls.Add(uiBtnTemp, 3, 0);

            uiPnHeader.Controls.Add(uiTpTop);

            Controls.Add(uiPnHeader, 0, 0);
            Controls.Add(uiTpField, 0, 1);

            uiBtnTemp.PerformClick(x2);
        }

        private void MoveSelectedDate(int direction)
        {
            selectedDateTime = uiBtnGroups.SelectedIndex switch
            {
                1 => selectedDateTime.AddDays(7 * direction),
                2 => selectedDateTime.AddMonths(direction),
                _ => selectedDateTime.AddDays(direction)
            };
            UpdateNavigatorText();
        }

        private void UpdateNavigatorText()
        {
            if (uiNavigator == null) return;
            uiNavigator.Text = selectedDateTime.ToString("yyyy-MM-dd HH:mm");
        }

        private void UiBtnTemp_Click(object? sender, EventArgs e)
        {
            if (sender is not AFMSButtonGroup buttonGroup || buttonGroup.SelectedItem?.Tag is not MaximizableTableLayoutType layoutType) return;

            uiTpField.SetLayout(layoutType, uiChart1, uiChart2, uiChart3, uiChart4);
        }

        private ChartSelectPanel CreateChartPanel()
        {
            ChartSelectPanel panel = new ChartSelectPanel();
            panel.Dock = DockStyle.Fill;
            panel.BorderRadius = 5;
            panel.Padding = Padding.Empty;
            panel.Margin = Padding.Empty;
            panel.BackColor = DllColorHelper.HexToColor("#F0F4F9");
            panel.uiTpBtnArr.BackColor = panel.BackColor;
            panel.BorderColor = DllColorHelper.GetCommonBorder();

            return panel;
        }
    }
}
