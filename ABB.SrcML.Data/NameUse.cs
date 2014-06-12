using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents the generalized use of a name. This does not distinguish whether the name represents a type, or variable, or what.
    /// </summary>
    public class NameUse : Expression {
        /// <summary>
        /// The name being used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prefix of the name. In a fully-qualified name like System.IO.File, the name is File and the prefix is System.IO.
        /// </summary>
        public NamePrefix Prefix { get; set; }

        /// <summary>
        /// The aliases active in the file at the point the name was used.
        /// </summary>
        public Collection<Alias> Aliases { get; set; }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            return string.Format("{0}{1}", Prefix, Name);
        }
    }
}
