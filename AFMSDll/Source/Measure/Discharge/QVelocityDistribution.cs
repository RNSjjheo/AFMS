namespace AFMSDll
{
    public sealed class QVelocityDistribution : _QBase
    {
        public QVelocityDistribution()
            : base(DischargeMethod.VeloDist)
        {
        }

        public override bool Calculate(out string error)
        {
            error = "유속분포법 계산이 아직 구현되지 않았습니다.";
            return false;
        }

        public static AFMSMathLabel GetExample()
        {
            AFMSMathLabel item = new AFMSMathLabel();
            item.ClearMath();
            item.AddText("Q = ");
            item.Add('∫');
            item.Add("A", AFMSMathTextType.Subscript);
            item.AddText(" u(x,y)dA");

            return item;
        }
    }
}
