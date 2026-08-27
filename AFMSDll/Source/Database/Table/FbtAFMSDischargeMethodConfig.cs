namespace AFMSDll
{
    public sealed class FbtAFMSDischargeMethodConfig : _FBTableBase
    {
        public const string TABLE_NAME = "AFMS_DISCHARGE_METHOD_CONFIG";
        public const string COL_DEVICE_TYPE = "DEVICE_TYPE";
        public const string COL_DEVICE_ID = "DEVICE_ID";
        public const string COL_DISCHARGE_METHOD = "DISCHARGE_METHOD";
        public const string COL_TRANSECT_CONFIG_ID = "TRANSECT_CONFIG_ID";
        public const string COL_CROSS_SECTION_ID = "CROSS_SECTION_ID";
        public const string COL_CONFIG_VERSION = "CONFIG_VERSION";
        public const string COL_CONFIG_JSON = "CONFIG_JSON";
        public const string COL_ENABLED = "ENABLED";
        public const string COL_CREATED_AT = "CREATED_AT";
        public const string COL_DESCRIPTION = "DESCRIPTION";

        public override string GetTableName() => TABLE_NAME;

        public override string GetCreateTableSql()
        {
            return $"""
                CREATE TABLE {TABLE_NAME} (
                  {COL_ID} INTEGER NOT NULL,
                  {COL_DEVICE_TYPE} VARCHAR(30) NOT NULL,
                  {COL_DEVICE_ID} INTEGER NOT NULL,
                  {COL_DISCHARGE_METHOD} VARCHAR(32) NOT NULL,
                  {COL_TRANSECT_CONFIG_ID} INTEGER,
                  {COL_CROSS_SECTION_ID} INTEGER,
                  {COL_CONFIG_VERSION} INTEGER NOT NULL,
                  {COL_CONFIG_JSON} BLOB SUB_TYPE TEXT CHARACTER SET UTF8 NOT NULL,
                  {COL_ENABLED} INTEGER DEFAULT 1 NOT NULL,
                  {COL_CREATED_AT} TIMESTAMP NOT NULL,
                  {COL_DESCRIPTION} VARCHAR(255),
                  CONSTRAINT PK_{TABLE_NAME} PRIMARY KEY ({COL_ID})
                )
                """;
        }
    }
}
