using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public sealed class FBColumnInfo
    {
        public string Name { get; set; } = "";
        public int FieldType { get; set; }
        public int FieldSubType { get; set; }
        public int FieldLength { get; set; }
        public int FieldScale { get; set; }
        public int CharacterLength { get; set; }
    }
}
