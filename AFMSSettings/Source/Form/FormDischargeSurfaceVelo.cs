using AFMSDll;
using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public class FormDischargeSurfaceVelo : _FormDischargeBase
    {
        private sealed class VersionOption
        {
            public VersionOption(string name, SurfaceVelocityVer0Control control)
            {
                Name = name;
                Control = control;
            }

            public string Name { get; }
            public SurfaceVelocityVer0Control Control { get; }

            public override string ToString() => Name;
        }

        private readonly SurfaceVelocityVer0Control _version0Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;

        public TabDiscSurfaceVelocity.SurfaceVelocityConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<TabDiscSurfaceVelocity.SurfaceVelocityConfig, string>? SaveHandler { get; set; }

        public FormDischargeSurfaceVelo()
        {
            Text = "지표유속법 설정";
            ClientSize = new Size(480, 720);

            _version0Control = new SurfaceVelocityVer0Control();
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

            if (SelectedVersion is VersionOption option)
                SetDetailControl(option.Control);
            else
                ClearDetailControl();
        }

        protected override void OnSaveRequested(EventArgs e)
        {
            if (HydroId < 0)
            {
                MessageBox.Show("저장할 유속계가 선택되지 않았습니다.", "입력 확인", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (SelectedVersion is not VersionOption option ||
                !option.Control.TryCreateConfig(HydroId, out TabDiscSurfaceVelocity.SurfaceVelocityConfig config))
                return;

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

            QueryBuilderSelect query = new QueryBuilderSelect();
            query.Table = FbtAFMSHydroMeter.TABLE_NAME;
            query.Add(FbtAFMSHydroMeter.COL_DEVICE_NAME);
            query.Add(FbtAFMSHydroMeter.COL_TRANSECT_CNT);
            query.Where(FbtAFMSHydroMeter.COL_ID, "=", HydroId);

            using FBDatabase db = new FBDatabase(FBProvider.Instance.ConnStrBuilder);
            DataTable table = db.Execute(query, out string error);

            if (!string.IsNullOrEmpty(error) || table.Rows.Count == 0) return;

            DataRow row = table.Rows[0];
            if (row[FbtAFMSHydroMeter.COL_DEVICE_NAME] != DBNull.Value)
                HydroMeterName = Convert.ToString(row[FbtAFMSHydroMeter.COL_DEVICE_NAME]) ?? string.Empty;

            if (row[FbtAFMSHydroMeter.COL_TRANSECT_CNT] == DBNull.Value) return;

            int transectCount = Convert.ToInt32(row[FbtAFMSHydroMeter.COL_TRANSECT_CNT]);
            if (transectCount > 0)
                _version0Control.SetCellRangeMaximum(transectCount);
        }
    }
}
