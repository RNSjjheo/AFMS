using AFMSSettings.Source.Form.Discharge;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace AFMSSettings
{
    public sealed class FormDischargeRatingCurve : _FormDischargeBase
    {
        private sealed class VersionOption
        {
            public VersionOption(RatingCurveVer0Control control) => Control = control;
            public RatingCurveVer0Control Control { get; }
            public override string ToString() => "Type1";
        }

        private readonly RatingCurveVer0Control _control;

        public TabDiscRatingCurve.RatingCurveConfig? ResultConfig { get; private set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Func<TabDiscRatingCurve.RatingCurveConfig, string>? SaveHandler { get; set; }

        public FormDischargeRatingCurve()
        {
            Text = "수위-유량 관계곡선 설정";
            ClientSize = new Size(560, 720);
            TargetLabelText = "설정 대상";
            HydroMeterName = "공통 수위-유량 관계곡선";

            _control = new RatingCurveVer0Control();
            AddVersion(new VersionOption(_control));
        }

        protected override void OnSelectedVersionChanged(EventArgs e)
        {
            base.OnSelectedVersionChanged(e);
            if (SelectedVersion is VersionOption option) SetDetailControl(option.Control);
        }

        protected override void OnSaveRequested(EventArgs e)
        {
            if (!_control.TryCreateConfig(out TabDiscRatingCurve.RatingCurveConfig config)) return;

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
    }
}
