using AFMSDll;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;

namespace AFMSSettings.Source.Form.Discharge
{
    public class FormDischargeMidSection : _FormDischargeBase
    {
        public sealed class MidSectionConfig
        {
            public int HydroId { get; set; } = -1;
            public int DisVer { get; set; }
            public int CellMin { get; set; }
            public int CellMax { get; set; }
            public double ConversionFactor { get; set; }
        }

        private sealed class VersionOption
        {
            public VersionOption(string name, MidSectionVer0Control control)
            {
                Name = name;
                Control = control;
            }

            public string Name { get; }
            public MidSectionVer0Control Control { get; }

            public override string ToString() => Name;
        }

        private readonly MidSectionVer0Control _version0Control;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int HydroId { get; set; } = -1;

        public MidSectionConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<MidSectionConfig, string>? SaveHandler { get; set; }

        public FormDischargeMidSection()
        {
            Text = "중간단면적법 설정";

            _version0Control = new MidSectionVer0Control();
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
                !option.Control.TryCreateConfig(HydroId, out MidSectionConfig config))
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

            using FBDatabase db = FBProvider.Instance.CreateDatabase();
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
