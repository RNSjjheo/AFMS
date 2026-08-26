using AFMSDll;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{

    public class TabDischarge : _TabBase
    {
        private sealed class HydroComboItem
        {
            public int Id { get; }
            public string DeviceName { get; }
            public int DeviceNo { get; }

            public HydroComboItem(int id, string deviceName, int deviceNo)
            {
                Id = id;
                DeviceName = deviceName;
                DeviceNo = deviceNo;
            }

            public override string ToString()
            {
                return DeviceName;
            }
        }


        private const string COMBO_TXT_MAPPING = "유량 산정 선택";
        private const string COMBO_TXT_CONFING = "매개 변수 설정";
        private const float COLUMN_1_WIDTH = 250F;

        private TableLayoutPanel uiTpThis;
        private AFMSButton uiButtonAccept;
        private AFMSComboBox uiComboMain;
        private AFMSComboBox uiComboMethod;
        private AFMSComboBox uiComboHydro;
        private AMFSHiddenTabControl uiTabMain;
        private TabDischargeMapping uiTabPageMapping;
        private TabDiscSurfaceVelocity uiTabPageSurfaceVelo;
        private TabDiscMidSection uiTabPageMidSection;
        private TabDiscVelocityDistribution uiTabPageVeloDist;
        private TabDiscRatingCurve uiTabPageRatingCurve;

        public TabDischarge()
        {
            Padding PADDING = new Padding(0, 4, 15, 4);
            Text = "유량산정";

            uiTpThis = new TableLayoutPanel();
            uiTpThis.Dock = DockStyle.Fill;
            uiTpThis.ColumnStyles.Clear();
            uiTpThis.RowStyles.Clear();
            uiTpThis.ColumnCount = 5;
            uiTpThis.RowCount = 2;
            uiTpThis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 350F));
            uiTpThis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_1_WIDTH));
            uiTpThis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, COLUMN_1_WIDTH));
            uiTpThis.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
            uiTpThis.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpThis.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpThis.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiComboMain = new AFMSComboBox();
            uiComboMain.BorderColor = DllColorHelper.HexToColor("#017D43");
            uiComboMain.Dock = DockStyle.Fill;
            uiComboMain.BorderRadius = 6;
            uiComboMain.BorderThickness = 1.5F;
            uiComboMain.Items.Add(COMBO_TXT_MAPPING);
            uiComboMain.Items.Add(COMBO_TXT_CONFING);
            uiComboMain.Margin = PADDING;

            uiComboMethod = new AFMSComboBox();
            uiComboMethod.BorderColor = DllColorHelper.HexToColor("#017D43");
            uiComboMethod.Dock = DockStyle.Fill;
            uiComboMethod.BorderRadius = 6;
            uiComboMethod.BorderThickness = 1.5F;
            uiComboMethod.Margin = PADDING;

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
            {
                if (method == DischargeMethod.None) continue;
                uiComboMethod.Items.Add(EnumPaser.GetKorString(method));
            }

            uiComboHydro = new AFMSComboBox();
            uiComboHydro.BorderColor = DllColorHelper.HexToColor("#017D43");
            uiComboHydro.Dock = DockStyle.Fill;
            uiComboHydro.BorderRadius = 6;
            uiComboHydro.BorderThickness = 1.5F;
            uiComboHydro.Margin = PADDING;
            uiComboHydro.PlaceholderText = "유속계 선택";

            uiButtonAccept = new AFMSButton();
            uiButtonAccept.Dock = DockStyle.Fill;
            uiButtonAccept.BorderRadius = 4;
            uiButtonAccept.Text = "저장";
            uiButtonAccept.BackColor = DllColorHelper.HexToColor("#02925D");
            uiButtonAccept.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiButtonAccept.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiButtonAccept.ForeColor = Color.White;
            uiButtonAccept.BorderThickness = 0F;
            uiButtonAccept.Margin = PADDING;

            uiTabMain = new AMFSHiddenTabControl();
            uiTabMain.Dock = DockStyle.Fill;

            uiTabPageMapping = new TabDischargeMapping();
            uiTabPageSurfaceVelo = new TabDiscSurfaceVelocity();
            uiTabPageMidSection = new TabDiscMidSection();
            uiTabPageRatingCurve = new TabDiscRatingCurve();
            uiTabPageVeloDist = new TabDiscVelocityDistribution();

            uiTabMain.TabPages.Add(uiTabPageMapping);
            uiTabMain.TabPages.Add(uiTabPageSurfaceVelo);
            uiTabMain.TabPages.Add(uiTabPageMidSection);
            uiTabMain.TabPages.Add(uiTabPageRatingCurve);
            uiTabMain.TabPages.Add(uiTabPageVeloDist);

            uiTpThis.Controls.Add(uiComboMain, 0, 0);
            uiTpThis.Controls.Add(uiComboMethod, 1, 0);
            uiTpThis.Controls.Add(uiComboHydro, 2, 0);
            uiTpThis.Controls.Add(uiButtonAccept, 3, 0);
            uiTpThis.Controls.Add(uiTabMain, 0, 1);
            uiTpThis.SetColumnSpan(uiTabMain, uiTpThis.ColumnCount);

            CtlMain = uiTpThis;
            uiTpMain.SetColumnSpan(CtlMain, 2);

            uiComboMain.SelectedIndexChanged += UiComboMain_SelectedIndexChanged;
            uiComboMethod.SelectedIndexChanged += UiComboMethod_SelectedIndexChanged;
            uiComboHydro.SelectedIndexChanged += UiComboHydro_SelectedIndexChanged;
            uiButtonAccept.Click += UiButtonAccept_Click;

            uiComboMain.SelectedItem = COMBO_TXT_MAPPING;

            foreach (TabPage page in uiTabMain.TabPages)
            {
                page.Padding = Padding.Empty;
                page.Margin = Padding.Empty;
                page.BackColor = Color.White;
            }
        }

        protected override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {

        }

        private void UiComboMain_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ClearDetailPanels();

            string text = uiComboMain.SelectedItem?.ToString() ?? string.Empty;

            switch (text)
            {
                case COMBO_TXT_MAPPING:
                    uiTpThis.ColumnStyles[1].Width = 0;
                    uiTpThis.ColumnStyles[2].Width = 0;
                    uiTabMain.SelectedTab = uiTabPageMapping;
                    uiComboMethod.SelectedIndex = 0;
                    break;

                case COMBO_TXT_CONFING:
                    uiTpThis.ColumnStyles[1].Width = COLUMN_1_WIDTH;
                    uiTpThis.ColumnStyles[2].Width = COLUMN_1_WIDTH;
                    if (uiComboMethod.SelectedIndex < 0 && uiComboMethod.Items.Count > 0) uiComboMethod.SelectedIndex = 0;
                    SelectMethodPage();
                    break;
            }

            uiButtonAccept.Visible = text == COMBO_TXT_MAPPING;
        }

        private void UiComboMethod_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ClearDetailPanels();
            SelectMethodPage();
        }

        private void UiComboHydro_SelectedIndexChanged(object? sender, EventArgs e)
        {
            ClearDetailPanels();

            // AFMS_DIS_ATTR_SURFACE_VELO.HYDRO_ID 연결 위치
            int hydroId = uiComboHydro.SelectedItem is HydroComboItem item ? item.Id : -1;
            uiTabPageSurfaceVelo.SetHydroId(hydroId);
            uiTabPageMidSection.SetHydroId(hydroId);
            uiTabPageVeloDist.SetHydroId(hydroId);
            SelectMethodPage();
        }

        private void ClearDetailPanels()
        {
            uiTabPageSurfaceVelo.ClearSelectionAndDetail();
            uiTabPageRatingCurve.ClearSelectionAndDetail();
        }

        private void SelectMethodPage()
        {
            if (!string.Equals(uiComboMain.SelectedItem?.ToString(), COMBO_TXT_CONFING, StringComparison.Ordinal)) return;

            string methodText = uiComboMethod.SelectedItem?.ToString() ?? string.Empty;

            foreach (DischargeMethod method in Enum.GetValues(typeof(DischargeMethod)))
            {
                if (method == DischargeMethod.None || !string.Equals(EnumPaser.GetKorString(method), methodText, StringComparison.Ordinal)) continue;

                switch (method)
                {
                    case DischargeMethod.SurfaceVelo:
                        uiTabMain.SelectedTab = uiTabPageSurfaceVelo;
                        uiTabPageSurfaceVelo.SetHydroId(uiComboHydro.SelectedItem is HydroComboItem item ? item.Id : -1);
                        break;
                    case DischargeMethod.MidSection:
                        uiTabMain.SelectedTab = uiTabPageMidSection;
                        uiTabPageMidSection.SetHydroId(uiComboHydro.SelectedItem is HydroComboItem midSectionItem ? midSectionItem.Id : -1);
                        break;
                    case DischargeMethod.VeloDist:
                        uiTabMain.SelectedTab = uiTabPageVeloDist;
                        uiTabPageVeloDist.SetHydroId(uiComboHydro.SelectedItem is HydroComboItem veloDistItem ? veloDistItem.Id : -1);
                        break;
                    case DischargeMethod.RatingCurve:
                        uiTabMain.SelectedTab = uiTabPageRatingCurve;
                        uiTabPageRatingCurve.LoadData();
                        break;
                }

                uiTpThis.ColumnStyles[2].Width = method == DischargeMethod.RatingCurve ? 0 : COLUMN_1_WIDTH;

                return;
            }
        }

        private void UiButtonAccept_Click(object? sender, EventArgs e)
        {
            if (uiTabMain.SelectedTab != uiTabPageMapping) return;

            string error = uiTabPageMapping.SaveChanges(out int savedCount);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (savedCount == 0)
            {
                MessageBox.Show("변경된 유속계 설정이 없습니다.", "유량산정", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show($"{savedCount}개의 유속계 설정을 저장했습니다.", "유량산정", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadHydroCombo()
        {
            int selectedHydroId = uiComboHydro.SelectedItem is HydroComboItem selectedItem ? selectedItem.Id : -1;

            uiComboHydro.ClearItems();

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.Add(FbtAFMSHydroMeter.COL_ID);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NO);
            query.OrderBy(FbtAFMSHydroMeter.COL_ID);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            int selectedIndex = -1;

            foreach (DataRow row in db.Results.Rows)
            {
                int id = row[FbtAFMSHydroMeter.COL_ID].ToInt();
                string deviceName = row[FbtAFMSHydroMeter.COL_DEVICE_NAME].ToText();
                int deviceNo = row[FbtAFMSHydroMeter.COL_DEVICE_NO].ToInt();

                uiComboHydro.Add(new HydroComboItem(id, deviceName, deviceNo));

                if (id == selectedHydroId) selectedIndex = uiComboHydro.Items.Count - 1;
            }

            if (selectedIndex >= 0) uiComboHydro.SelectedIndex = selectedIndex;
            else if (uiComboHydro.Items.Count > 0) uiComboHydro.SelectedIndex = 0;
        }

        protected override void ThisPageEntered(object? sender, EventArgs e)
        {
            uiTabPageMapping.LoadData();
            LoadHydroCombo();
        }
    }
}
