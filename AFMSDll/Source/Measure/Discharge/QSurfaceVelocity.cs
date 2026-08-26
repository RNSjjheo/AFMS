namespace AFMSDll
{
    public sealed class QSurfaceVelocity : _QBase
    {
        public const string VER1_ATTR_NODE1 = "Max Vi";
        public const string VER1_ATTR_NODE2 = "a";
        public const string VER1_ATTR_NODE3 = "b";
        public DiscVerSurfaceVelo Version { get; set; }
        public QSurfaceVelocity() : base(DischargeMethod.SurfaceVelo) { }
    }
}
