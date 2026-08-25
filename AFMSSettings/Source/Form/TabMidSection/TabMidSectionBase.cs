using AFMSDll;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public abstract class TabMidSectionBase : TabPage
    {
        public DiscVerMidSection Version;
        public TableLayoutPanel uiTpMainRow;
        public AFMSNumberBox uiNumberCellMin;
        public AFMSNumberBox uiNumberCellMax;
        public AFMSNumberBox uiNumberConversionFactor;
        public AFMSMathLabel uiLbExample;
        public Label uiDesc;

        protected TabMidSectionBase()
        {
            BackColor = Color.White;
            Padding = Padding.Empty;
            Margin = Padding.Empty;
        }

        public override string ToString()
        {
            switch (Version)
            {
                case DiscVerMidSection.Ver00:
                    return "Type1";
                default:
                    return "정의되지 않음";
            }
        }
    }
}
