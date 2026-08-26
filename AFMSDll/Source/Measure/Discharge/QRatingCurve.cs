namespace AFMSDll
{
    public sealed class QRatingCurve : _QBase
    {
        public const string VER1_ATTR_MAX_H = "max h";
        public const string VER1_ATTR_NODE1 = "a";
        public const string VER1_ATTR_NODE2 = "b";
        public const string VER1_ATTR_NODE3 = "c";
        public QRatingCurve() : base(DischargeMethod.RatingCurve) { }
    }
}
