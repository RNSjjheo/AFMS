using AFMSDll;

namespace AFMSDischargeService
{
    internal sealed class QVelocityDistributionCalculator : QCalculatorBase
    {
        public QVelocityDistributionCalculator(DateTime calculationStartTime)
            : base(DischargeMethod.VeloDist, calculationStartTime)
        {
        }

        public override bool Calculate(out string error)
        {
            error = "유속분포법 계산이 아직 구현되지 않았습니다.";
            return false;
        }

    }
}
