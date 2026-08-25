using AFMSDll;
using log4net.Layout;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class TabDiscMidSection : _TabDischargeBase
    {
        private AFMSDataGridView uiGridDepth;
        private AFMSDataGridView uiGridWidth;
        private AFMSDataGridView uiGridVertical;

        public TabDiscMidSection()
        {
            Text = "중간단면적법";
            BackColor = Color.White;

            uiTpMain.ColumnStyles[0].Width = 35F;
            uiTpMain.ColumnStyles[1].Width = 65F;

            uiGridMain.AFMSHeaderHeight = 24;
            uiGridMain.AFMSRowHeight = 34;
            uiGridMain.BorderRadius = 6;
            uiGridMain.MergedHeaderLineColor = Color.FromArgb(245, 246, 248);
            uiGridMain.MergedHeaderLineThickness = 0.5F;

            SetupUncertaintyPanel();
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not AFMSDataGridView grid) return;

            grid.ClearMergedHeaders();

            SetColumnVisible(grid, FbtAFMSDiscAttrMidSection.COL_ID, false);

            SetColumnStyle(grid, COL_NO, "No.", 20F);
            SetColumnStyle(grid, FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN, "MIN", 27F);
            SetColumnStyle(grid, FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX, "MAX", 27F);
            SetColumnStyle(grid, "환산계수", "환산계수", 26F, "0.000");

            if (grid.Columns.Contains(FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN) && grid.Columns.Contains(FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX))
                grid.AddMergedHeader("분석범위", FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN, FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX);

            grid.ClearSelection();
            grid.CurrentCell = null;
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
        }

        protected override void _TabDischargeBase_Enter(object? sender, EventArgs e)
        {
            LoggingData();
        }

        private void SetupUncertaintyPanel()
        {
            AFMSSectionPanel gpucert = new AFMSSectionPanel();
            gpucert.Dock = DockStyle.Fill;
            gpucert.HeaderText = "중간단면적 불확도 정보";
            gpucert.HeaderHeight = 38;
            gpucert.HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6");
            gpucert.HeaderLineColor = DllColorHelper.HexToColor("#244B37");
            gpucert.Padding = Padding.Empty;
            gpucert.Margin = Padding.Empty;

            TableLayoutPanel content = gpucert.ContentLayout;
            content.Margin = Padding.Empty;
            content.Padding = new Padding(14, 12, 14, 14);
            content.ColumnCount = 2;
            content.RowCount = 2;
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            content.BackColor = Color.White;

            AFMSPanel depthPanel = CreateUncertaintySection("수심에 따른 불확도", out uiGridDepth);
            AFMSPanel widthPanel = CreateUncertaintySection("하폭에 따른 불확도", out uiGridWidth);
            AFMSPanel verticalPanel = CreateUncertaintySection("측선개수에 따른 불확도", out uiGridVertical);

            depthPanel.Margin = new Padding(0, 0, 0, 4);
            widthPanel.Margin = new Padding(0, 4, 0, 0);
            verticalPanel.Margin = new Padding(5, 0, 0, 0);

            content.Controls.Add(depthPanel, 0, 0);
            content.Controls.Add(widthPanel, 0, 1);
            content.Controls.Add(verticalPanel, 1, 0);
            content.SetRowSpan(verticalPanel,2);

            CtlSub = gpucert;

            SetupDepthGrid();
            SetupWidthGrid();
            SetupVerticalGrid();
        }

        private AFMSPanel CreateUncertaintySection(string titleText, out AFMSDataGridView grid)
        {
            AFMSPanel panel = new AFMSPanel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(5);
            panel.BackColor = Color.White;
            panel.BorderColor = DllColorHelper.GetCommonBorder();
            panel.BorderThickness = 0F;
            panel.BorderRadius = 7;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            Label title = new Label();
            title.Dock = DockStyle.Fill;
            title.Margin = Padding.Empty;
            title.Text = titleText;
            title.TextAlign = ContentAlignment.MiddleLeft;
            title.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Bold);
            title.ForeColor = DllColorHelper.HexToColor("#138052");

            grid = CreateUncertaintyGrid();

            layout.Controls.Add(title, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            panel.Controls.Add(layout);

            return panel;
        }

        private static AFMSDataGridView CreateUncertaintyGrid()
        {
            AFMSDataGridView grid = new AFMSDataGridView();
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToResizeRows = false;
            grid.ShowSelectedRowHighlight = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AFMSHeaderHeight = 31;
            grid.AFMSRowHeight = 30;
            grid.BorderRadius = 5;
            grid.BorderThickness = 1F;
            grid.BorderColor = DllColorHelper.GetCommonBorder();
            grid.BackgroundColor = Color.White;
            grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.ScrollBars = ScrollBars.None;
            grid.ClearSelection();

            return grid;
        }

        private void SetupDepthGrid()
        {
            AddColumn(uiGridDepth, "DEPTH_RANGE", "깊이 범위 (m)", 24F);
            AddColumn(uiGridDepth, "ABS_UNCERT", "절대 불확도 (m)", 24F);
            AddColumn(uiGridDepth, "REL_UNCERT", "상대 불확도 (%)", 24F);

            uiGridDepth.Rows.Add("0.4 ~ 6", "0.02", "0.65");
            uiGridDepth.Rows.Add("6 ~ 14", "0.025", "0.25");
            uiGridDepth.ClearSelection();
        }

        private void SetupWidthGrid()
        {
            AddColumn(uiGridWidth, "WIDTH_RANGE", "하폭 범위 (m)", 34F);
            AddColumn(uiGridWidth, "ABS_ERROR", "절대 오차 (m)", 33F);
            AddColumn(uiGridWidth, "REL_UNCERT", "상대 불확도 (%)", 33F);

            uiGridWidth.Rows.Add("0 ~ 100", "0 ~ 0.15", "0.15");
            uiGridWidth.Rows.Add("101 ~ 150", "0.15 ~ 0.25", "0.20");
            uiGridWidth.Rows.Add("151 ~ 250", "0.3 ~ 0.6", "0.25");
            uiGridWidth.ClearSelection();
        }

        private void SetupVerticalGrid()
        {
            AddColumn(uiGridVertical, "VERTICAL_COUNT", "측선 수", 50F);
            AddColumn(uiGridVertical, "UNCERTAINTY", "불확도 (%)", 50F);

            uiGridVertical.Rows.Add("5", "7.5");
            uiGridVertical.Rows.Add("10", "4.5");
            uiGridVertical.Rows.Add("15", "3.0");
            uiGridVertical.Rows.Add("20", "2.5");
            uiGridVertical.Rows.Add("25", "2.0");
            uiGridVertical.Rows.Add("30", "1.5");
            uiGridVertical.Rows.Add("35", "1.0");
            uiGridVertical.Rows.Add("40", "1.0");
            uiGridVertical.Rows.Add("45", "1.0");
            uiGridVertical.ClearSelection();
        }

        private static void AddColumn(DataGridView grid, string name, string headerText, float fillWeight)
        {
            DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
            column.Name = name;
            column.HeaderText = headerText;
            column.FillWeight = fillWeight;
            column.ReadOnly = true;
            column.SortMode = DataGridViewColumnSortMode.NotSortable;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.Columns.Add(column);
        }

        private string LoggingData()
        {
            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSDiscAttrMidSection.TABLE_NAME;

            query.Add(FbtAFMSDiscAttrMidSection.COL_ID);
            query.Add(FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MIN);
            query.Add(FbtAFMSDiscAttrMidSection.COL_CELL_RANGE_MAX);
            query.AsAlias(FbtAFMSDiscAttrMidSection.COL_CONVERSION_FACTOR, "환산계수");

            query.Where(FbtAFMSDiscAttrMidSection.COL_HYDRO_ID, "=", 0);
            query.OrderBy(FbtAFMSDiscAttrMidSection.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error)) return error;

            table.AddRowNo(COL_NO);
            uiGridMain.DataSource = table;

            return string.Empty;
        }
    }
}
