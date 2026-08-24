using AFMSDll;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace AFMSSettings
{
    public class TabHydroMeter : _TabBase
    {
        private const string COL_HYDRO_ID = "__HYDRO_ID";
        private const string COL_ROW_NO = "번호";
        private const string COL_DEV_TYPE = "유속계";
        private const string COL_DEV_CONFIG = "연결정보";
        private const string COL_TRANSECT_CNT = "측선수";
        private const string COL_TRANSECT_SETTING = "측선설정";

        private TableLayoutPanel uiTpRigth;
        private AFMSGuidePanel uiGuide;
        private AFMSCategoryPanel uiPnDetail;
        private AFMSDataGridView uiGridDetail;
        private AFMSButton uiBtnAdd;

        public TabHydroMeter()
        {
            Text = "유속계";
            Desc = "통합 자동유량측정시스템의 유속계를 관리합니다.";

            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.ColumnCount = 2;
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            uiBtnAdd = new AFMSButton();
            uiBtnAdd.Dock = DockStyle.Fill;
            uiBtnAdd.BorderRadius = 4;
            uiBtnAdd.Text = "추가하기";
            uiBtnAdd.BackColor = DllColorHelper.HexToColor("#02925D");
            uiBtnAdd.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiBtnAdd.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiBtnAdd.ForeColor = Color.White;
            uiBtnAdd.BorderThickness = 0F;
            uiBtnAdd.Click += Button_Click;

            uiTpRigth = new TableLayoutPanel();
            uiTpRigth.Dock = DockStyle.Fill;
            uiTpRigth.ColumnStyles.Clear();
            uiTpRigth.RowStyles.Clear();
            uiTpRigth.ColumnCount = 1;
            uiTpRigth.RowCount = 1;
            uiTpRigth.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpRigth.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpRigth.Padding = Padding.Empty;
            uiTpRigth.Margin = Padding.Empty;

            uiGuide = new AFMSGuidePanel();
            uiGuide.Dock = DockStyle.Fill;
            uiGuide.BackColor = DllColorHelper.HexToColor("#FAFDFA");
            uiGuide.Margin = Padding.Empty;
            uiGuide.Title = "설정 안내";
            uiGuide.Add(GuideLevelType.Level0, "통합자동유량측정시스템에서 운영할 유속계를 설정합니다.");

            uiGridDetail = new AFMSDataGridView();
            uiGridDetail.Dock = DockStyle.Fill;
            uiGridDetail.CheckBoxCheckedBorderColor = Color.FromArgb(30, 160, 80);
            uiGridDetail.CheckBoxCheckedBorderThickness = 1.7F;
            uiGridDetail.AFMSHeaderHeight = 42;
            uiGridDetail.AFMSRowHeight = 54;
            uiGridDetail.BorderRadius = 8;
            uiGridDetail.DataBindingComplete += BindingDetailComplete;

            uiPnDetail = new AFMSCategoryPanel();
            uiPnDetail.CategoryText = "측선 정보 상세";
            uiPnDetail.Dock = DockStyle.Fill;
            uiPnDetail.HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6");
            uiPnDetail.HeaderForeColor = DllColorHelper.HexToColor("#244B37");
            uiPnDetail.BorderRadius = 8;
            uiPnDetail.Padding = new Padding(10);
            uiPnDetail.Margin = Padding.Empty;

            uiPnDetail.Controls.Add(uiGridDetail);
            uiTpRigth.Controls.Add(uiPnDetail, 0, 0);

            CtlMain = uiGridMain;
            CtlSub = uiTpRigth;

            uiGridMain.CellClick += UiGridMain_CellClick;
            uiGridMain.SelectionChanged += UiGridList_SelectionChanged;
            uiTpMain.Controls.Add(uiBtnAdd, 0, 1);
            uiTpMain.SetRowSpan(uiTpRigth, 2);
        }

        protected override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (uiGridMain.Columns.Contains(COL_HYDRO_ID)) uiGridMain.Columns[COL_HYDRO_ID].Visible = false;
            SetupTransectInputColumn();
        }

        private void SetupTransectInputColumn()
        {
            if (uiGridMain.Columns.Contains(COL_TRANSECT_SETTING)) uiGridMain.Columns.Remove(COL_TRANSECT_SETTING);

            AFMSDataGridViewButtonColumn column = new AFMSDataGridViewButtonColumn();
            column.Name = COL_TRANSECT_SETTING;
            column.HeaderText = COL_TRANSECT_SETTING;
            column.Text = "측선입력 ";
            column.BackColor = DllColorHelper.HexToColor("#02925D");
            column.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            column.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            column.ForeColor = Color.White;
            column.BorderColor = Color.Transparent;
            column.BorderThickness = 0F;
            column.BorderRadius = 4;
            column.ButtonMargin = new Padding(10, 9, 10, 9);

            uiGridMain.Columns.Add(column);
            column.DisplayIndex = uiGridMain.Columns.Count - 1;
        }

        private void UiGridMain_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (uiGridMain.Columns[e.ColumnIndex].Name != COL_TRANSECT_SETTING) return;
            if (uiGridMain.Rows[e.RowIndex].DataBoundItem is not DataRowView rowView) return;

            TransectInput(rowView.Row);
        }

        private void TransectInput(DataRow row)
        {
            int hydroId = Convert.ToInt32(row[COL_HYDRO_ID]);
            int transectCount = Convert.ToInt32(row[COL_TRANSECT_CNT]);

            if (transectCount <= 0)
            {
                MessageBox.Show("설정된 측선수가 없습니다.", "측선 입력", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FormTransectInput form = new FormTransectInput(hydroId, transectCount);
            if (form.ShowDialog(FindForm()) != DialogResult.OK) return;

            ShowDetail(row);
        }

        protected void BindingDetailComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
        }

        private void UiGridList_SelectionChanged(object? sender, EventArgs e)
        {
            if (uiGridMain.CurrentRow?.DataBoundItem is not DataRowView rowView)
            {
                ClearDetail();
                return;
            }

            ShowDetail(rowView.Row);
        }

        private void ClearDetail()
        {
            uiGridDetail.DataSource = CreateTransectDetailTable();
        }

        private void ShowDetail(DataRow row)
        {
            if (!row.Table.Columns.Contains(COL_HYDRO_ID) || row[COL_HYDRO_ID] == DBNull.Value)
            {
                ClearDetail();
                return;
            }

            int hydroId = Convert.ToInt32(row[COL_HYDRO_ID]);
            string sql = $"SELECT FIRST 1 {FbtAFMSHydroTransect.COL_DISTANCE_DATAS}";
            sql += "\n" + $"FROM {FbtAFMSHydroTransect.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtAFMSHydroTransect.COL_HYDRO_ID} = {hydroId}";
            sql += "\n" + $"ORDER BY {FbtAFMSHydroTransect.COL_ID} DESC";

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.RunQuery(sql);

            if (db.Results.Rows.Count == 0)
            {
                ClearDetail();
                return;
            }

            string json = Convert.ToString(db.Results.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS])?.Trim() ?? "";
            if (string.IsNullOrEmpty(json))
            {
                ClearDetail();
                return;
            }

            try
            {
                List<double>? distances = JsonSerializer.Deserialize<List<double>>(json);
                if (distances == null || distances.Count == 0)
                {
                    ClearDetail();
                    return;
                }

                DataTable table = CreateTransectDetailTable();
                for (int i = 0; i < distances.Count; i++) table.Rows.Add(i + 1, distances[i]);
                uiGridDetail.DataSource = table;
            }
            catch (JsonException)
            {
                ClearDetail();
            }
        }

        private DataTable CreateTransectDetailTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("측선 번호", typeof(int));
            table.Columns.Add("좌안에서의 거리(m)", typeof(double));
            return table;
        }

        private void Button_Click(object? sender, EventArgs e)
        {
            using FormHydorMeter form = new FormHydorMeter();
            if (form.ShowDialog(FindForm()) != DialogResult.OK) return;

            string error = AddHydroMeter(form.HydroType, form.CommConfig, form.TransectCount);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "유속계 추가 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            FBProvider.Instance.Sync();
            LoadHydroMeterList();
        }

        private string AddHydroMeter(HydroMeterType hydroType, string commConfig, int transectCount)
        {
            DateTime now = DateTime.Now;

            QueryBuilderInsert query = new QueryBuilderInsert();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.AutoIncrement = FbtAFMSHydroMeter.COL_ID;
            query.Value(FbtAFMSHydroMeter.COL_MEASURE_DATE, now.ToString("yyyyMMdd"));
            query.Value(FbtAFMSHydroMeter.COL_MEASURE_TIME, now.ToString("HHmmss"));
            query.Value(FbtAFMSHydroMeter.COL_DEVICE_NAME, hydroType.ToString());
            query.Value(FbtAFMSHydroMeter.COL_DEVICE_NO, 1);
            query.Value(FbtAFMSHydroMeter.COL_COMM_CONFIG, commConfig);
            query.Value(FbtAFMSHydroMeter.COL_DEVICE_ATTR, "");
            query.Value(FbtAFMSHydroMeter.COL_TRANSECT_CNT, transectCount);
            query.Value(FbtAFMSHydroMeter.COL_AFMS_ONLY, 1);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.Execute(query, out string error);
            return error;
        }

        private void LoadHydroMeterList()
        {
            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.AsAlias(FbtAFMSHydroMeter.COL_ID, COL_HYDRO_ID);
            query.AsAlias(FbtAFMSHydroMeter.COL_DEVICE_NAME, COL_DEV_TYPE);
            query.AsAlias(FbtAFMSHydroMeter.COL_COMM_CONFIG, COL_DEV_CONFIG);
            query.AsAlias(FbtAFMSHydroMeter.COL_TRANSECT_CNT, COL_TRANSECT_CNT);
            query.OrderBy(FbtAFMSHydroMeter.COL_AFMS_ONLY);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            db.RunQuery(query.Sql);
            db.Results.AddRowNo(COL_ROW_NO);
            uiGridMain.DataSource = db.Results;
        }

        protected override void ThisPageEntered(object? sender, EventArgs e)
        {
            LoadHydroMeterList();
        }
    }
}