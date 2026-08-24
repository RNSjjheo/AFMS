using AFMSDll;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormSurfaceVelocityInput : AFMSForm
    {
        private TableLayoutPanel uiTpMainRow;
        private AFMSButton uiButtonSave;
        private AFMSButton uiButtonCancel;
        private AFMSComboBox uiCbVersion;
        private AMFSHiddenTabControl uiTabMain;
        private TabSurfaceVeloVer0 uiTabPageVer0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;
        public TabDiscSurfaceVelocity.SurfaceVelocityConfig? ResultConfig { get; private set; }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<TabDiscSurfaceVelocity.SurfaceVelocityConfig, string>? SaveHandler { get; set; }

        public FormSurfaceVelocityInput()
        {
            Text = "지표유속 설정 입력";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            BackColor = Color.White;
            ClientSize = new Size(480, 720);
            Padding = new Padding(18);

            SetupMainLayout();
            SetupVersion();
            SetupButtons();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SetDefaultCellRange();
        }

        private void SetDefaultCellRange()
        {
            if (HydroId < 0) return;

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.Add(FbtAFMSHydroMeter.COL_TRANSECT_CNT);
            query.Where(FbtAFMSHydroMeter.COL_ID, "=", HydroId);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0) return;

            object value = table.Rows[0][FbtAFMSHydroMeter.COL_TRANSECT_CNT];
            if (value == DBNull.Value) return;

            int transectCount = Convert.ToInt32(value);
            if (transectCount < 1) return;

            foreach (TabPage tabPage in uiTabMain.TabPages)
            {
                if (tabPage is not TabSurfaceBase page) continue;

                page.uiNumberCellMax.Maximum = transectCount;
                page.uiNumberCellMax.SetValue(transectCount);
            }
        }

        private void SetupMainLayout()
        {
            uiTpMainRow = new TableLayoutPanel();
            uiTpMainRow.Dock = DockStyle.Fill;
            uiTpMainRow.Margin = Padding.Empty;
            uiTpMainRow.Padding = new Padding(5);
            uiTpMainRow.ColumnCount = 1;
            uiTpMainRow.RowCount = 3;
            uiTpMainRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMainRow.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            Controls.Add(uiTpMainRow);
        }

        private void SetupVersion()
        {
            uiCbVersion = new AFMSComboBox();
            uiCbVersion.Dock = DockStyle.Right;
            uiCbVersion.Width = 200;
            uiCbVersion.BorderRadius = 5;
            uiCbVersion.BorderColor = DllColorHelper.GetCommonBorder();

            uiTabPageVer0 = new TabSurfaceVeloVer0();

            uiTabMain = new AMFSHiddenTabControl();
            uiTabMain.Dock = DockStyle.Fill;

            uiCbVersion.Items.Add(uiTabPageVer0);
            uiTabMain.TabPages.Add(uiTabPageVer0);
            uiTabMain.SelectedTab = uiTabPageVer0;
            uiCbVersion.SelectedIndexChanged += UiCbVersion_SelectedIndexChanged;
            uiCbVersion.SelectedIndex = 0;

            foreach (TabPage page in uiTabMain.TabPages)
            {
                page.Padding = Padding.Empty;
                page.Margin = Padding.Empty;
                page.BackColor = Color.White;
            }

            uiTpMainRow.Controls.Add(uiCbVersion, 0, 0);
            uiTpMainRow.Controls.Add(uiTabMain, 0, 1);
        }

        private void UiCbVersion_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (uiCbVersion.SelectedItem is not TabSurfaceBase page) return;
            uiTabMain.SelectedTab = page;
        }

        private void SetupButtons()
        {
            uiButtonSave = CreateButton("저장", true);
            uiButtonCancel = CreateButton("취소", false);
            uiButtonSave.Click += UiButtonSave_Click;
            uiButtonCancel.Click += UiButtonCancel_Click;

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(uiButtonCancel, 2, 0);
            layout.Controls.Add(uiButtonSave, 3, 0);

            CancelButton = uiButtonCancel;
            uiTpMainRow.Controls.Add(layout, 0, 2);
        }

        private void UiButtonSave_Click(object? sender, EventArgs e)
        {
            if (HydroId < 0)
            {
                MessageBox.Show("저장할 유속계가 선택되지 않았습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TabDiscSurfaceVelocity.SurfaceVelocityConfig? config = CreateConfig();
            if (config == null) return;

            config.HydroId = HydroId;

            if (SaveHandler != null)
            {
                string error = SaveHandler(config);
                if (!string.IsNullOrEmpty(error))
                {
                    MessageBox.Show(error, "저장 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            ResultConfig = config;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void UiButtonCancel_Click(object? sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private TabDiscSurfaceVelocity.SurfaceVelocityConfig? CreateConfig()
        {
            if (uiTabMain.SelectedTab is not TabSurfaceBase page) return null;

            if (!page.uiNumberCellMin.IntValue.HasValue || !page.uiNumberCellMax.IntValue.HasValue)
            {
                MessageBox.Show("CELL Min과 CELL Max를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (page.uiNumberCellMin.IntValue.Value > page.uiNumberCellMax.IntValue.Value)
            {
                MessageBox.Show("CELL Min은 CELL Max보다 클 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (!page.uiNumberVst.DoubleValue.HasValue)
            {
                MessageBox.Show("Vst 불확도 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                page.uiNumberVst.Focus();
                return null;
            }

            if (!page.uiNumberVindex.DoubleValue.HasValue)
            {
                MessageBox.Show("Vindex 불확도 값을 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                page.uiNumberVindex.Focus();
                return null;
            }

            if (page.uiGrid.Rows.Count == 0)
            {
                MessageBox.Show("Max Vi, a, b 값을 한 구간 이상 추가해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            foreach (DataGridViewRow row in page.uiGrid.Rows)
            {
                if (!TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE1, out _) ||
                    !TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE2, out _) ||
                    !TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE3, out _))
                {
                    MessageBox.Show($"{row.Index + 1}번 구간에 빈 데이터가 있습니다. Max Vi, a, b 값을 모두 입력해주세요.",
                        "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return null;
                }
            }

            TabDiscSurfaceVelocity.SurfaceVelocityConfig config = new TabDiscSurfaceVelocity.SurfaceVelocityConfig();
            config.DisVer = (int)page.Version;
            config.CellMin = page.uiNumberCellMin.IntValue.Value;
            config.CellMax = page.uiNumberCellMax.IntValue.Value;
            config.UcertVst = page.uiNumberVst.DoubleValue.Value;
            config.UcertVindex = page.uiNumberVindex.DoubleValue.Value;

            foreach (DataGridViewRow row in page.uiGrid.Rows)
            {
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE1, out double maxVi);
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE2, out double a);
                TryGetGridDouble(row, QSurfaceVelo.VER1_ATTR_NODE3, out double b);

                config.Coefficients.Add(new TabDiscSurfaceVelocity.SurfaceVelocityCoefficient { MaxVi = maxVi, A = a, C = b });
            }

            config.CoeffCount = config.Coefficients.Count;
            return config;
        }

        private static bool TryGetGridDouble(DataGridViewRow row, string columnName, out double value)
        {
            value = 0;

            object? cellValue = row.Cells[columnName].Value;
            if (cellValue == null || cellValue == DBNull.Value) return false;

            string text = Convert.ToString(cellValue)?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(text)) return false;

            return double.TryParse(text, out value);
        }

        private static AFMSButton CreateButton(string text, bool primary)
        {
            AFMSButton button = new AFMSButton();
            button.Dock = DockStyle.Fill;
            button.Margin = new Padding(8, 0, 0, 0);
            button.Text = text;
            button.BorderRadius = 5;

            if (primary)
            {
                button.BackColor = DllColorHelper.HexToColor("#02925D");
                button.HoverBackColor = DllColorHelper.HexToColor("#027F51");
                button.PressedBackColor = DllColorHelper.HexToColor("#026D46");
                button.ForeColor = Color.White;
                button.BorderThickness = 0F;
            }
            else
            {
                button.BackColor = Color.White;
                button.HoverBackColor = DllColorHelper.HexToColor("#F3F6F4");
                button.PressedBackColor = DllColorHelper.HexToColor("#E7ECE9");
                button.ForeColor = DllColorHelper.HexToColor("#4C5751");
                button.BorderColor = DllColorHelper.HexToColor("#C9D2CD");
                button.BorderThickness = 1F;
            }

            return button;
        }
    }
}
