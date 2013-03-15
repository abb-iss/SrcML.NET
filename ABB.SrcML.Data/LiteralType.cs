using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// An enumeration of the different kinds of literals
    /// </summary>
    public enum LiteralKind {
        /// <summary>String literal</summary>
        String,
        /// <summary>Boolean literal</summary>
        Boolean,
        /// <summary>Character literal</summary>
        Character,
        /// <summary>Number literal</summary>
        Number,
    }
}
