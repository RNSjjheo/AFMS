namespace AFMSDll
{
    public sealed class QSurfaceVelocity : _QBase
    {
        public const string VER1_ATTR_NODE1 = "Max Vi";
        public const string VER1_ATTR_NODE2 = "a";
        public const string VER1_ATTR_NODE3 = "b";

        private DiscVerSurfaceVelo Version;

        public QSurfaceVelocity(): base(DischargeMethod.SurfaceVelo)
        {
        }

        public override bool Calculate(out string error)
        {
            error = "표면유속법 계산이 아직 구현되지 않았습니다.";
            return false;
        }

        public static AFMSMathLabel GetExample(DiscVerSurfaceVelo version)
        {
            AFMSMathLabel item = new AFMSMathLabel();
            item.ClearMath();
            item.Add('V');
            item.Add("m", AFMSMathTextType.Subscript);
            item.AddText(" = ");
            item.Add(VER1_ATTR_NODE2);
            item.AddText(" × ");
            item.Add('V');
            item.Add("i", AFMSMathTextType.Subscript);
            item.AddText(" + ");
            item.Add(VER1_ATTR_NODE3);

            item.NewLine();

            item.AddText("Q = ");
            item.AddText("V");
            item.Add("m", AFMSMathTextType.Subscript);
            item.AddText(" × A(h)");
            return item;
        }
    }
}
