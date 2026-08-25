using AFMSDll;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormMidSection : AFMSForm
    {
        public sealed class MidSectionConfig
        {
            public int HydroId { get; set; } = -1;
            public int DisVer { get; set; }
            public int CellMin { get; set; }
            public int CellMax { get; set; }
            public double ConversionFactor { get; set; }
        }

        private TableLayoutPanel uiTpMainRow;
        private AFMSButton uiButtonSave;
        private AFMSButton uiButtonCancel;
        private AFMSComboBox uiCbVersion;
        private AMFSHiddenTabControl uiTabMain;
        private TabMidSectionVer0 uiTabPageVer0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;

        public MidSectionConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<MidSectionConfig, string>? SaveHandler { get; set; }

        public FormMidSection()
        {
            Text = "중간단면적법 설정 입력";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            BorderRadius = 8;
            ShowWindowShadow = true;
            ContentBackColor = Color.White;
            ClientSize = new Size(480, 420);
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

            uiTabPageVer0 = new TabMidSectionVer0();

            uiTabMain = new AMFSHiddenTabControl();
            uiTabMain.Dock = DockStyle.Fill;

            AddVersionPage(uiTabPageVer0);
            uiTabMain.SelectedTab = uiTabPageVer0;
            uiCbVersion.SelectedIndexChanged += UiCbVersion_SelectedIndexChanged;
            uiCbVersion.SelectedIndex = 0;

            uiTpMainRow.Controls.Add(uiCbVersion, 0, 0);
            uiTpMainRow.Controls.Add(uiTabMain, 0, 1);
        }

        private void AddVersionPage(TabMidSectionBase page)
        {
            page.Padding = Padding.Empty;
            page.Margin = Padding.Empty;
            page.BackColor = Color.White;
            uiCbVersion.Items.Add(page);
            uiTabMain.TabPages.Add(page);
        }

        private void UiCbVersion_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (uiCbVersion.SelectedItem is not TabMidSectionBase page) return;
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
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
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
                if (tabPage is not TabMidSectionBase page) continue;

                page.uiNumberCellMin.Maximum = transectCount;
                page.uiNumberCellMax.Maximum = transectCount;
                page.uiNumberCellMax.SetValue(transectCount);
            }
        }

        private void UiButtonSave_Click(object? sender, EventArgs e)
        {
            if (HydroId < 0)
            {
                MessageBox.Show("저장할 유속계가 선택되지 않았습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            MidSectionConfig? config = CreateConfig();
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

        private MidSectionConfig? CreateConfig()
        {
            if (uiTabMain.SelectedTab is not TabMidSectionBase page) return null;

            if (!page.uiNumberCellMin.IntValue.HasValue || !page.uiNumberCellMax.IntValue.HasValue)
            {
                MessageBox.Show("MIN과 MAX를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (page.uiNumberCellMin.IntValue.Value > page.uiNumberCellMax.IntValue.Value)
            {
                MessageBox.Show("MIN은 MAX보다 클 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return null;
            }

            if (!page.uiNumberConversionFactor.DoubleValue.HasValue)
            {
                MessageBox.Show("환산계수를 입력해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                page.uiNumberConversionFactor.Focus();
                return null;
            }

            MidSectionConfig config = new MidSectionConfig();
            config.DisVer = (int)page.Version;
            config.CellMin = page.uiNumberCellMin.IntValue.Value;
            config.CellMax = page.uiNumberCellMax.IntValue.Value;
            config.ConversionFactor = Math.Round(page.uiNumberConversionFactor.DoubleValue.Value, 2, MidpointRounding.AwayFromZero);
            return config;
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
