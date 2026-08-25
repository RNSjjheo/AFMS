using AFMSDll;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class _FormDischargeBase : AFMSForm
    {
        private readonly Label _uiLabelHydroMeterName;
        private readonly Label _uiLabelTarget;
        private Control? _detailControl;
        private string _hydroMeterName = string.Empty;

        protected readonly TableLayoutPanel uiTpMainRow;
        protected readonly AFMSComboBox uiCbVersion;
        protected readonly Panel uiPanelDetail;
        protected readonly AFMSButton uiButtonSave;
        protected readonly AFMSButton uiButtonCancel;

        public _FormDischargeBase()
        {
            Text = "유속산정법 설정";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            ShowMinimizeButton = false;
            ShowMaximizeButton = false;
            ShowInfoButton = false;
            ShowInTaskbar = false;
            BorderRadius = 8;
            ShowWindowShadow = true;
            ContentBackColor = Color.White;
            ClientSize = new Size(480, 620);
            Padding = new Padding(18);

            uiTpMainRow = CreateMainLayout();
            Controls.Add(uiTpMainRow);

            TableLayoutPanel contextLayout = CreateContextLayout();
            AFMSPanel hydroMeterPanel = CreateHydroMeterPanel(out _uiLabelHydroMeterName);
            uiCbVersion = CreateVersionComboBox();

            _uiLabelTarget = CreateContextLabel("설정 대상 유속계");
            contextLayout.Controls.Add(_uiLabelTarget, 0, 0);
            contextLayout.Controls.Add(CreateContextLabel("산정법 버전"), 1, 0);
            contextLayout.Controls.Add(hydroMeterPanel, 0, 1);
            contextLayout.Controls.Add(uiCbVersion, 1, 1);

            uiPanelDetail = new Panel();
            uiPanelDetail.Dock = DockStyle.Fill;
            uiPanelDetail.Margin = Padding.Empty;
            uiPanelDetail.Padding = Padding.Empty;
            uiPanelDetail.BackColor = Color.White;

            uiButtonSave = CreateButton("저장", true);
            uiButtonCancel = CreateButton("취소", false);

            TableLayoutPanel actionLayout = CreateActionLayout();
            actionLayout.Controls.Add(uiButtonCancel, 2, 0);
            actionLayout.Controls.Add(uiButtonSave, 3, 0);

            uiTpMainRow.Controls.Add(contextLayout, 0, 0);
            uiTpMainRow.Controls.Add(uiPanelDetail, 0, 1);
            uiTpMainRow.Controls.Add(actionLayout, 0, 2);

            uiCbVersion.SelectedIndexChanged += UiCbVersion_SelectedIndexChanged;
            uiButtonSave.Click += UiButtonSave_Click;
            uiButtonCancel.Click += UiButtonCancel_Click;
            CancelButton = uiButtonCancel;

            UpdateHydroMeterName();
        }

        [Category("AFMS Data")]
        [DefaultValue("")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string HydroMeterName
        {
            get => _hydroMeterName;
            set
            {
                _hydroMeterName = value?.Trim() ?? string.Empty;
                UpdateHydroMeterName();
            }
        }

        [Category("AFMS Data")]
        [DefaultValue("설정 대상 유속계")]
        public string TargetLabelText
        {
            get => _uiLabelTarget.Text;
            set => _uiLabelTarget.Text = value ?? string.Empty;
        }

        [Browsable(false)]
        public object? SelectedVersion => uiCbVersion.SelectedItem;

        [Browsable(false)]
        public Control? DetailControl => _detailControl;

        public event EventHandler? SaveRequested;

        protected void AddVersion(object version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            uiCbVersion.Items.Add(version);
            if (uiCbVersion.SelectedIndex < 0) uiCbVersion.SelectedIndex = 0;
        }

        protected void ClearVersions()
        {
            uiCbVersion.Items.Clear();
        }

        protected void SelectVersion(int index)
        {
            if (index < -1 || index >= uiCbVersion.Items.Count) throw new ArgumentOutOfRangeException(nameof(index));
            uiCbVersion.SelectedIndex = index;
        }

        protected void SetDetailControl(Control control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
            if (ReferenceEquals(_detailControl, control)) return;

            if (_detailControl != null)
            {
                if (_detailControl is Form previousForm) previousForm.Hide();
                uiPanelDetail.Controls.Remove(_detailControl);
            }

            if (control is Form childForm)
            {
                if (childForm.Visible) childForm.Hide();
                childForm.TopLevel = false;
                childForm.FormBorderStyle = FormBorderStyle.None;
                childForm.ShowInTaskbar = false;
            }

            control.Dock = DockStyle.Fill;
            control.Margin = Padding.Empty;
            _detailControl = control;
            uiPanelDetail.Controls.Add(control);
            control.BringToFront();

            if (control is Form form) form.Show();
            else control.Show();
        }

        protected void ClearDetailControl()
        {
            if (_detailControl == null) return;

            if (_detailControl is Form form) form.Hide();
            uiPanelDetail.Controls.Remove(_detailControl);
            _detailControl = null;
        }

        protected virtual void OnSelectedVersionChanged(EventArgs e)
        {
        }

        protected virtual void OnSaveRequested(EventArgs e)
        {
            SaveRequested?.Invoke(this, e);
        }

        protected void CompleteSave()
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        protected virtual void CancelSettings()
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void UiCbVersion_SelectedIndexChanged(object? sender, EventArgs e)
        {
            OnSelectedVersionChanged(e);
        }

        private void UiButtonSave_Click(object? sender, EventArgs e)
        {
            OnSaveRequested(e);
        }

        private void UiButtonCancel_Click(object? sender, EventArgs e)
        {
            CancelSettings();
        }

        private void UpdateHydroMeterName()
        {
            if (_uiLabelHydroMeterName == null) return;

            _uiLabelHydroMeterName.Text = string.IsNullOrEmpty(HydroMeterName)
                ? "유속계를 선택해주세요."
                : HydroMeterName;
        }

        private static TableLayoutPanel CreateMainLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(5);
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
            return layout;
        }

        private static TableLayoutPanel CreateContextLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 2;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 27F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 39F));
            return layout;
        }

        private static Label CreateContextLabel(string text)
        {
            Label label = new Label();
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.Padding = new Padding(6, 0, 0, 0);
            label.Text = text;
            label.TextAlign = ContentAlignment.BottomLeft;
            label.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9F, FontStyle.Regular);
            label.ForeColor = DllColorHelper.HexToColor("#69737D");
            return label;
        }

        private static AFMSPanel CreateHydroMeterPanel(out Label nameLabel)
        {
            AFMSPanel panel = new AFMSPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 3, 8, 2);
            panel.Padding = Padding.Empty;
            panel.BorderRadius = 6;
            panel.BorderThickness = 1F;
            panel.BorderColor = DllColorHelper.HexToColor("#CDD7D1");
            panel.BackColor = DllColorHelper.HexToColor("#F8FAF9");

            Label iconLabel = new Label();
            iconLabel.Dock = DockStyle.Left;
            iconLabel.Width = 42;
            iconLabel.Margin = Padding.Empty;
            iconLabel.Text = "◴";
            iconLabel.TextAlign = ContentAlignment.MiddleCenter;
            iconLabel.Font = new Font("Segoe UI Symbol", 15F, FontStyle.Regular);
            iconLabel.ForeColor = DllColorHelper.HexToColor("#02925D");

            nameLabel = new Label();
            nameLabel.Dock = DockStyle.Fill;
            nameLabel.Margin = Padding.Empty;
            nameLabel.Padding = Padding.Empty;
            nameLabel.TextAlign = ContentAlignment.MiddleLeft;
            nameLabel.AutoEllipsis = true;
            nameLabel.Font = new Font(DLLStyle.DEFAULT_FONT_SYLTE, 9.5F, FontStyle.Regular);
            nameLabel.ForeColor = DllColorHelper.HexToColor("#34433C");

            panel.Controls.Add(nameLabel);
            panel.Controls.Add(iconLabel);
            return panel;
        }

        private static AFMSComboBox CreateVersionComboBox()
        {
            AFMSComboBox comboBox = new AFMSComboBox();
            comboBox.Dock = DockStyle.Fill;
            comboBox.Margin = new Padding(8, 3, 0, 2);
            comboBox.BorderRadius = 6;
            comboBox.BorderColor = DllColorHelper.HexToColor("#BFCBD3");
            return comboBox;
        }

        private static TableLayoutPanel CreateActionLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = new Padding(0, 8, 0, 0);
            layout.ColumnCount = 4;
            layout.RowCount = 1;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            return layout;
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
