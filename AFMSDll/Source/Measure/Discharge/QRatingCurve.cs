namespace AFMSDll
{
    public sealed class QRatingCurve : _QBase
    {
        public const string VER1_ATTR_MAX_H = "max h";
        public const string VER1_ATTR_NODE1 = "a";
        public const string VER1_ATTR_NODE2 = "b";
        public const string VER1_ATTR_NODE3 = "c";
        public QRatingCurve() : base(DischargeMethod.RatingCurve) { }

        public static AFMSMathLabel GetExample(DiscVerRatingCurve version)
        {
            AFMSMathLabel item = new();
            item.ClearMath();
            item.Add("Q = ");
            item.Add(VER1_ATTR_NODE1);
            item.Add(" × (h - ");
            item.Add(VER1_ATTR_NODE2);
            item.Add(")");
            item.Add(VER1_ATTR_NODE3, AFMSMathTextType.Superscript);
            return item;
        }
    }
}
