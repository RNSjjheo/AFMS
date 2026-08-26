using AFMSDll;

namespace AFMSSettings
{
    internal class TabCrossSection : _TabBase
    {
        private readonly CrossSectionChartPanel _chartPanel;
        private readonly CrossSectionManagePanel _managePanel;

        public TabCrossSection()
        {
            Text = "단면설정";
            Desc = "csv 형태의 단면 자료를 입력합니다";

            _chartPanel = new CrossSectionChartPanel();
            _managePanel = new CrossSectionManagePanel();
            _managePanel.CrossSectionSelected += ManagePanel_CrossSectionSelected;

            CtlMain = _chartPanel;
            CtlSub = _managePanel;
        }

        protected override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
        }

        protected override void ThisPageEntered(object? sender, EventArgs e)
        {
            _managePanel.RefreshList();
            _chartPanel.RefreshHydroMeters();
        }

        private void ManagePanel_CrossSectionSelected(object? sender, CrossSectionDataEventArgs e)
        {
            _chartPanel.SetData(e.Data);
        }
    }
}
