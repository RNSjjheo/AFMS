using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    internal class FBTableSchema
    {
        public bool Exists { get; set; }

        public string Error { get; set; } = "";

        public List<FBColumnSchema> Columns { get; }
            = new List<FBColumnSchema>();
    }
}
