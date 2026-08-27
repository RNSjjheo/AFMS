using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QSurfaceVelocityCalculator : QCalculatorBase
    {
        public const string VER1_ATTR_NODE1 = "Max Vi";
        public const string VER1_ATTR_NODE2 = "a";
        public const string VER1_ATTR_NODE3 = "b";

        private DiscVerSurfaceVelo Version;

        public QSurfaceVelocityCalculator(): base(DischargeMethod.SurfaceVelo)
        {
        }

        public override bool Calculate(out string error)
        {
            error = "표면유속법 계산이 아직 구현되지 않았습니다.";
            return false;
        }

    }
}
