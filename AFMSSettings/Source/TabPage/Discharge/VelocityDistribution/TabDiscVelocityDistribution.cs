using AFMSDll;
using System.Drawing;

namespace AFMSSettings
{
    internal sealed class TabDiscVelocityDistribution : _TabDischargeBase
    {
        private readonly VelocityDistributionConfigListPanel _configListPanel;
        private readonly VelocityDistributionTransectPanel _transectPanel;
        private int _hydroId = -1;

        public TabDiscVelocityDistribution()
            : base(true)
        {
            Text = "유속분포법";
            BackColor = Color.White;
            uiTpMain.ColumnStyles[0].Width = 75F;
            uiTpMain.ColumnStyles[1].Width = 25F;

            _configListPanel = new VelocityDistributionConfigListPanel();
            _transectPanel = new VelocityDistributionTransectPanel();
            _configListPanel.SelectedTransectNosChanged += ConfigListPanel_SelectedTransectNosChanged;
            CtlMain = _configListPanel;
            CtlSub = _transectPanel;
        }

        public void SetHydroId(int hydroId)
        {
            if (_hydroId == hydroId) return;
            _hydroId = hydroId;
            _configListPanel.SetHydroId(hydroId);

            string error = _transectPanel.SetHydroId(hydroId);
            if (string.IsNullOrEmpty(error)) error = _configListPanel.LoadData();
            ShowLoadError(error);
        }

        public string LoadData()
        {
            string error = _transectPanel.SetHydroId(_hydroId);
            if (!string.IsNullOrEmpty(error)) return error;
            return _configListPanel.LoadData();
        }

        public override void BindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
        }

        protected override void UiButtonInput_Click(object? sender, EventArgs e)
        {
            if (_hydroId < 0)
            {
                MessageBox.Show("유속계를 먼저 선택해주세요.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using FormDischargeVelocityDistribution form = new();
            form.HydroId = _hydroId;
            form.SaveHandler = _configListPanel.SaveConfig;
            if (form.ShowDialog(FindForm()) != DialogResult.OK || form.ResultConfig == null) return;
            ShowLoadError(_configListPanel.LoadData());
        }

        protected override void OnPageActivated()
        {
            ShowLoadError(LoadData());
        }

        private void ConfigListPanel_SelectedTransectNosChanged(object? sender, IReadOnlyList<int> transectNos)
        {
            _transectPanel.ShowTransects(transectNos);
        }

        private static void ShowLoadError(string error)
        {
            if (!string.IsNullOrEmpty(error))
                MessageBox.Show(error, "유속분포 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
