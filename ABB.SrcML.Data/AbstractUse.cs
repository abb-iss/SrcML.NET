using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents the 
    /// </summary>
    public abstract class AbstractUse {
        /// <summary>
        /// The location of this use in the original source file and in srcML
        /// </summary>
        public SourceLocation Location { get; set; }

        /// <summary>
        /// The name being used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope that contains this use
        /// </summary>
        public Scope ParentScope { get; set; }

        /// <summary>
        /// The programming language for this scope
        /// </summary>
        public Language ProgrammingLanguage { get; set; }
    }
}
