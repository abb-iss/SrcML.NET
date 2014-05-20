using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class NameUse : Expression {
        /// <summary>
        /// The name being used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prefix of the name. In a fully-qualified name like System.IO.File, the name is File and the prefix is System.IO.
        /// </summary>
        public NamePrefix Prefix { get; set; }
    }
}
