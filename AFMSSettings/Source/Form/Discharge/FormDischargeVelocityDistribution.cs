using AFMSDll;
using System.ComponentModel;
using System.Data;
using System.Drawing;

namespace AFMSSettings
{
    public sealed class FormDischargeVelocityDistribution : _FormDischargeBase
    {
        public sealed class VelocityDistributionConfig
        {
            public int HydroId { get; set; } = -1;
            public int DisVer { get; set; }
            public double Phi { get; set; }
            public double HorizontalGridM { get; set; }
            public double VerticalGridM { get; set; }
            public double MaxVelocityDepthRatio { get; set; }
            public VelocityDistributionFitMode FitMode { get; set; }
            public int MinimumPositiveMeasurements { get; set; }
            public double? FlowCenterX { get; set; }
            public double? BetaLeft { get; set; }
            public double? BetaRight { get; set; }
            public List<int> TransectNos { get; set; } = new();
        }

        private sealed class VersionOption
        {
            public VersionOption(string name, VelocityDistributionVer0Control control)
            {
                Name = name;
                Control = control;
            }

            public string Name { get; }
            public VelocityDistributionVer0Control Control { get; }
            public override string ToString() => Name;
        }

        private readonly VelocityDistributionVer0Control _version0Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;

        public VelocityDistributionConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<VelocityDistributionConfig, string>? SaveHandler { get; set; }

        public FormDischargeVelocityDistribution()
        {
            Text = "유속분포법 설정";
            ClientSize = new Size(760, 620);
            _version0Control = new VelocityDistributionVer0Control();
            AddVersion(new VersionOption("Type1", _version0Control));
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadHydroMeterContext();
        }

        protected override void OnSelectedVersionChanged(EventArgs e)
        {
            base.OnSelectedVersionChanged(e);
            if (SelectedVersion is VersionOption option) SetDetailControl(option.Control);
            else ClearDetailControl();
        }

        protected override void OnSaveRequested(EventArgs e)
        {
            if (HydroId < 0)
            {
                MessageBox.Show("저장할 유속계가 선택되지 않았습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (SelectedVersion is not VersionOption option ||
                !option.Control.TryCreateConfig(HydroId, out VelocityDistributionConfig config)) return;

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
            CompleteSave();
        }

        private void LoadHydroMeterContext()
        {
            if (HydroId < 0) return;

            QueryBuilderSelect hydroQuery = new();
            hydroQuery.Table = FbtAFMSHydroMeter.TABLE_NAME;
            hydroQuery.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            hydroQuery.Where(FbtAFMSHydroMeter.COL_ID, "=", HydroId);

            using FBDatabase db = new(FBProvider.Instance.ConnStrBuilder);
            DataTable hydroTable = db.Execute(hydroQuery, out string error);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "유속계 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (hydroTable.Rows.Count > 0)
                HydroMeterName = Convert.ToString(hydroTable.Rows[0][FbtAFMSHydroMeter.COL_DEVICE_NAME]) ?? string.Empty;

            QueryBuilderSelect transectQuery = new();
            transectQuery.Table = FbtAFMSHydroTransect.TABLE_NAME;
            transectQuery.First = 1;
            transectQuery.Add(FbtAFMSHydroTransect.COL_DISTANCE_DATAS);
            transectQuery.Where(FbtAFMSHydroTransect.COL_HYDRO_ID, "=", HydroId);
            transectQuery.OrderByDesc(FbtAFMSHydroTransect.COL_ID);

            DataTable transectTable = db.Execute(transectQuery, out error);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(error, "측선 설정 조회 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (transectTable.Rows.Count == 0 || transectTable.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS] == DBNull.Value)
            {
                MessageBox.Show("유속계에 설정된 측선 정보가 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string json = Convert.ToString(transectTable.Rows[0][FbtAFMSHydroTransect.COL_DISTANCE_DATAS]) ?? string.Empty;
            if (!TransectBuilder.TryBuild(json, out TransectCollection transects))
            {
                MessageBox.Show("측선 설정을 읽을 수 없습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _version0Control.SetTransects(transects);
        }
    }
}
