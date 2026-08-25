using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSSettings
{
    public abstract class _TabDischargeBase :TabPage
    {
        protected const string COL_NO = "NO";
        protected abstract void _TabDischargeBase_Enter(object? sender, EventArgs e);
        public abstract void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e);
        protected abstract void UiButtonInput_Click(object? sender, EventArgs e);
        private Control _SubControl;
        private Control _MainControl;
        private readonly TableLayoutPanel? _mainHostLayout;
        public AFMSDataGridView uiGridMain;
        public TableLayoutPanel uiTpMain;
        public AFMSButton uiButtonInput;
        public AFMSGuidePanel? uiLatestDataGuide;

        protected _TabDischargeBase(bool showLatestDataGuide = true)
        {
            this.Enter += _TabDischargeBase_Enter;
            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnCount = 2;
            uiTpMain.RowCount = 2;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            uiGridMain = new AFMSDataGridView();
            uiGridMain.Dock = DockStyle.Fill;
            uiGridMain.CheckBoxCheckedBorderColor = Color.FromArgb(30, 160, 80);
            uiGridMain.CheckBoxCheckedBorderThickness = 1.7F;
            uiGridMain.AFMSHeaderHeight = 42;
            uiGridMain.AFMSRowHeight = 54;
            uiGridMain.BorderRadius = 8;
            uiGridMain.DataBindingComplete += BindingComplete;
            uiGridMain.Margin = Padding.Empty;

            uiButtonInput = new AFMSButton();
            uiButtonInput.Dock = DockStyle.Right;
            uiButtonInput.Width = 100;
            uiButtonInput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            uiButtonInput.Size = new Size(110, 36);
            uiButtonInput.Text = "입력";
            uiButtonInput.BorderRadius = 5;
            uiButtonInput.BackColor = DllColorHelper.HexToColor("#02925D");
            uiButtonInput.HoverBackColor = DllColorHelper.HexToColor("#027F51");
            uiButtonInput.PressedBackColor = DllColorHelper.HexToColor("#026D46");
            uiButtonInput.ForeColor = Color.White;
            uiButtonInput.BorderThickness = 0F;
            uiButtonInput.Click += UiButtonInput_Click;

            uiTpMain.Controls.Add(uiButtonInput,1,1);

            if (showLatestDataGuide)
            {
                _mainHostLayout = CreateMainHostLayout();
                uiLatestDataGuide = CreateLatestDataGuide();
                _mainHostLayout.Controls.Add(uiLatestDataGuide, 0, 1);
                uiTpMain.Controls.Add(_mainHostLayout, 0, 0);
            }

            CtlMain = uiGridMain;

            Controls.Add(uiTpMain);
        }


        protected static void SetColumnVisible(DataGridView grid, string columnName, bool visible)
        {
            if (grid.Columns.Contains(columnName)) grid.Columns[columnName].Visible = visible;
        }

        protected static void SetColumnStyle(DataGridView grid, string columnName, string headerText, float fillWeight, string format = "")
        {
            if (!grid.Columns.Contains(columnName)) return;

            DataGridViewColumn column = grid.Columns[columnName];
            column.HeaderText = headerText;
            column.FillWeight = fillWeight;
            column.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            if (!string.IsNullOrEmpty(format)) column.DefaultCellStyle.Format = format;
        }



        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control CtlMain
        {
            get => _MainControl;
            set
            {
                if (_MainControl != null)
                {
                    if (_mainHostLayout != null) _mainHostLayout.Controls.Remove(_MainControl);
                    else uiTpMain.Controls.Remove(_MainControl);
                }

                _MainControl = value;

                if (_MainControl == null) return;

                _MainControl.Dock = DockStyle.Fill;
                _MainControl.Margin = new Padding(0,5,0,5);

                if (_mainHostLayout != null) _mainHostLayout.Controls.Add(_MainControl, 0, 0);
                else uiTpMain.Controls.Add(_MainControl, 0, 0);
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Control CtlSub
        {
            get => _SubControl;
            set
            {
                if (_SubControl != null) uiTpMain.Controls.Remove(_SubControl);

                _SubControl = value;

                if (_SubControl == null) return;

                _SubControl.Dock = DockStyle.Fill;
                _SubControl.Margin = new Padding(5);

                uiTpMain.Controls.Add(_SubControl, 1, 0);
            }
        }

        private static TableLayoutPanel CreateMainHostLayout()
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.Margin = Padding.Empty;
            layout.Padding = Padding.Empty;
            layout.ColumnCount = 1;
            layout.RowCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 82F));
            return layout;
        }

        private static AFMSGuidePanel CreateLatestDataGuide()
        {
            AFMSGuidePanel guide = new AFMSGuidePanel();
            guide.Dock = DockStyle.Fill;
            guide.Margin = new Padding(0, 5, 0, 5);
            guide.Padding = new Padding(15, 3, 18, 8);
            guide.Title = "유량 산정 안내";
            guide.BorderColor = DllColorHelper.GetCommonBorder();
            guide.BorderRadius = 6;
            guide.BorderThickness = 1F;
            guide.IconColor = DllColorHelper.HexToColor("#02925D");
            guide.Add(GuideLevelType.Level0, "가장 최근 데이터가 유량 산정에 사용됩니다.");
            return guide;
        }
    }
}
