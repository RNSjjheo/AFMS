using AFMSDll;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;

namespace AFMSDataViewer
{
    public partial class InfoSites : UserControl
    {
        private TableLayoutPanel uiTpMain;
        private AFMSLabel uiLbCodeKey;
        private AFMSLabel uiLbCode;
        private AFMSLabel uiLbNameKey;
        private AFMSLabel uiLbName;
        private AFMSLabel uiLbPeroidKey;
        private AFMSLabel uiLbPeroid;

        public InfoSites()
        {
            InitializeComponent();

            uiPnMain.HeaderText = "지점 정보";
            uiTpMain = uiPnMain.ContentLayout;

            uiTpMain.ColumnStyles.Clear();
            uiTpMain.RowStyles.Clear();
            uiTpMain.RowCount = 3;
            uiTpMain.ColumnCount = 2;
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F));
            uiTpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3));
            uiTpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / 3));
            uiTpMain.Padding = new Padding(0, 2, 0, 2);
            uiTpMain.Margin = Padding.Empty;

            uiLbCodeKey = CreateLable(true, "지점코드");
            uiLbNameKey = CreateLable(true, "지점 명");
            uiLbPeroidKey = CreateLable(true, "측정주기");
            
            uiLbCode = CreateLable(false, "");
            uiLbName = CreateLable(false, "");
            uiLbPeroid = CreateLable(false, "10분 간격");

            uiTpMain.Controls.Add(uiLbCodeKey, 0, 0);
            uiTpMain.Controls.Add(uiLbCode, 1, 0);
            uiTpMain.Controls.Add(uiLbNameKey, 0, 1);
            uiTpMain.Controls.Add(uiLbName, 1, 1);
            uiTpMain.Controls.Add(uiLbPeroidKey, 0, 2);
            uiTpMain.Controls.Add(uiLbPeroid, 1, 2);
        }

        private AFMSLabel CreateLable(bool isKey, string keyStr)
        {
            AFMSLabel label = new AFMSLabel();

            label.Dock = DockStyle.Fill;
            label.BackColor = Color.White;
            label.Text = keyStr;
            label.BorderThickness = 0;
            label.BorderRadius = 0;
            label.BorderStyle = BorderStyle.None;
            label.Margin = new Padding(12, 0, 12, 0);
            label.Padding = Padding.Empty;
            label.CharacterSpacing = -0.5F;

            if (isKey)
            {
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Font = new Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular, GraphicsUnit.Point);
                label.ForeColor = DllColorHelper.GetDescStrColor();
            }
            else
            {
                label.TextAlign = ContentAlignment.MiddleRight;
                label.Font = new Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, GraphicsUnit.Point);
                label.ForeColor = Color.Black;
            }

            return label;
        }

        public void ReadDatabase()
        {
            string sql = $"SELECT {FbtSETUP.COL_VALUE01}, ";
            sql += "\n" + $"CAST({FbtSETUP.COL_VALUE02} AS VARCHAR(100) CHARACTER SET KSC_5601) AS {FbtSETUP.COL_VALUE02}";
            sql += "\n" + $"FROM {FbtSETUP.TABLE_NAME}";
            sql += "\n" + $"WHERE {FbtSETUP.COL_PK1} = 1";
            sql += "\n" + $"AND {FbtSETUP.COL_PK2} = 1";

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
            db.RunQuery(sql);

            foreach (DataRow row in db.Results.Rows)
            {
                string code = row[FbtSETUP.COL_VALUE01].ToString() ?? string.Empty;
                string name = row[FbtSETUP.COL_VALUE02].ToString() ?? string.Empty;

                uiLbCode.Text = code;
                uiLbName.Text = name;

                return;
            }
        }
    }
}
