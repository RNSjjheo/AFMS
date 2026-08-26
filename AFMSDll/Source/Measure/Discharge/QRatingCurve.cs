namespace AFMSDll
{
    public sealed class QRatingCurve : _QBase
    {
        public const string VER1_ATTR_MAX_H = "max h";
        public const string VER1_ATTR_NODE1 = "a";
        public const string VER1_ATTR_NODE2 = "b";
        public const string VER1_ATTR_NODE3 = "c";

        public QRatingCurve()
            : base(DischargeMethod.RatingCurve)
        {
        }

        public override bool Calculate(out string error)
        {
            error = "수위-유량곡선법 계산이 아직 구현되지 않았습니다.";
            return false;
        }

        public static AFMSMathLabel GetExample(DiscVerRatingCurve version)
        {
            AFMSMathLabel item = new AFMSMathLabel();
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
