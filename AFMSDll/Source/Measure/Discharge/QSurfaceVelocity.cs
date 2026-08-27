namespace AFMSDll
{
    public sealed class QSurfaceVelocity : _QBase
    {
        public const string VER1_ATTR_NODE1 = "Max Vi";
        public const string VER1_ATTR_NODE2 = "a";
        public const string VER1_ATTR_NODE3 = "b";
        public DiscVerSurfaceVelo Version { get; set; }
        public QSurfaceVelocity() : base(DischargeMethod.SurfaceVelo) { }

        public static AFMSMathLabel GetExample(DiscVerSurfaceVelo version)
        {
            AFMSMathLabel item = new();
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
