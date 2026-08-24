using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSSettings
{
    public abstract class _TabBase:TabPage
    {
        protected const float WIDTH_LEFT_PANEL = 450F;
        protected abstract void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e);
        protected abstract void ThisPageEntered(object? sender, EventArgs e);
        private Control _SubControl;
        private Control _MainControl;
        private Label uiLbDesc;
        public AFMSDataGridView uiGridMain;
        public TableLayoutPanel uiTpMain;
        public _TabBase()
        {
            Enter += ThisPageEntered;

            uiTpMain = new TableLayoutPanel();
            uiTpMain.Dock = DockStyle.Fill;
            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowStyles.Clear();
            uiTpMain.ColumnCount = 2;
            uiTpMain.RowCount = 1;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            uiLbDesc = new Label();
            uiLbDesc.Dock = DockStyle.Fill;
            uiLbDesc.TextAlign = ContentAlignment.MiddleLeft;
            uiLbDesc.AutoSize = false;
            uiLbDesc.Font = new("맑은 고딕", 10F, FontStyle.Regular);
            uiLbDesc.ForeColor = DllColorHelper.GetDescStrColor();
            uiLbDesc.Padding = new Padding(10, 10, 10, 0);

            uiGridMain = new AFMSDataGridView();
            uiGridMain.Dock = DockStyle.Fill;
            uiGridMain.CheckBoxCheckedBorderColor = Color.FromArgb(30, 160, 80);
            uiGridMain.CheckBoxCheckedBorderThickness = 1.7F;
            uiGridMain.AFMSHeaderHeight = 42;
            uiGridMain.AFMSRowHeight = 54;
            uiGridMain.BorderRadius = 8;
            uiGridMain.DataBindingComplete += BindingComplete;

            Controls.Add(uiTpMain);
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
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]

        public Control CtlMain
        {
            get => _MainControl;
            set
            {
                if (_SubControl != null) uiTpMain.Controls.Remove(_MainControl);

                _MainControl = value;

                if (_MainControl == null) return;

                _MainControl.Dock = DockStyle.Fill;
                _MainControl.Margin = new Padding(5);

                uiTpMain.Controls.Add(_MainControl, 0, 0);
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string Desc
        {
            get => uiLbDesc.Text;
            set => uiLbDesc.Text = value;
        }

    }
}
