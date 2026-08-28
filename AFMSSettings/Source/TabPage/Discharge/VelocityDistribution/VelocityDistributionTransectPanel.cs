using AFMSDll;
using System.Data;
using System.Drawing;

namespace AFMSSettings
{
    internal sealed class VelocityDistributionTransectPanel : UserControl
    {
        private readonly AFMSDataGridView _grid;
        private TransectCollection _transects = new();

        public VelocityDistributionTransectPanel()
        {
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;
            BackColor = Color.White;

            AFMSSectionPanel section = new();
            section.Dock = DockStyle.Fill;
            section.SectionStyle = AFMSSectionStyle.FilledHeader;
            section.HeaderText = "선택된 운영 측선";
            section.HeaderHeight = 38;
            section.HeaderBackColor = DllColorHelper.HexToColor("#F5F8F6");
            section.HeaderColor = DllColorHelper.HexToColor("#244B37");

            _grid = CreateGrid();
            section.ContentLayout.Controls.Add(_grid);
            Controls.Add(section);
        }

        public string SetHydroId(int hydroId)
        {
            _grid.Rows.Clear();
            _transects = new TransectCollection();
            if (hydroId < 0) return string.Empty;

            QueryBuilderSelect query = new();
            query.Table = FbtAFMSHydroTransect.TABLE_NAME;
            query.First = 1;
            query.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            query.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", hydroId);
            query.OrderByDesc(FbtAFMSHydroTransect.COL_ID);

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            DataTable table = db.Execute(query, out string error);
            if (!string.IsNullOrEmpty(error)) return error;
            if (table.Rows.Count == 0) return string.Empty;

            string json = table.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS].ToText();
            if (!string.IsNullOrEmpty(json) && !TransectBuilder.TryBuild(json, out _transects))
                return "측선 설정을 읽을 수 없습니다.";
            return string.Empty;
        }

        public void ShowTransects(IReadOnlyList<int> selectedNos)
        {
            _grid.Rows.Clear();
            foreach (int no in selectedNos)
            {
                Transect? transect = _transects.FirstOrDefault(item => item.No == no);
                _grid.Rows.Add(no, transect == null ? "-" : $"{transect.CenterLeftBankDistance:0.##} m");
            }
            _grid.ClearSelection();
        }

        private static AFMSDataGridView CreateGrid()
        {
            AFMSDataGridView grid = new();
            grid.Dock = DockStyle.Fill;
            grid.Margin = Padding.Empty;
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.ShowSelectedRowHighlight = false;
            grid.AFMSHeaderHeight = 36;
            grid.AFMSRowHeight = 38;
            grid.BorderRadius = 6;

            DataGridViewTextBoxColumn noColumn = new();
            noColumn.HeaderText = "측선 번호";
            noColumn.FillWeight = 45F;
            noColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            DataGridViewTextBoxColumn distanceColumn = new();
            distanceColumn.HeaderText = "거리";
            distanceColumn.FillWeight = 55F;
            distanceColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            grid.Columns.Add(noColumn);
            grid.Columns.Add(distanceColumn);
            return grid;
        }
    }
}
