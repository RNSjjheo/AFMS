namespace AFMSDll
{
    public sealed class QVelocityDistribution : _QBase
    {
        public QVelocityDistribution()
            : base(DischargeMethod.VeloDist)
        {
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
