using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents an import or using directive (usually found at the top of a source file)
    /// </summary>
    public class Alias {
        public NamespaceUse ImportedNamespace { get; set; }

        public NamedScopeUse ImportedNamedScope { get; set; }

        /// <summary>
        /// Returns true if this is a namespace alias (true if the name is not set).
        /// </summary>
        public bool IsNamespaceAlias { get { return ImportedNamedScope == null; } }

        /// <summary>
        /// The location of this alias in both the source file and the XML.
        /// </summary>
        public SourceLocation Location { get; set; }

        public Language ProgrammingLanguage { get; set; }

        /// <summary>
        /// Constructs a new alias object
        /// </summary>
        public Alias() {
            this.ImportedNamedScope = null;
            this.ImportedNamespace = null;
        }

        /// <summary>
        /// Checks if this is a valid alias for the given type use. Namespace prefixes are always valid.
        /// Other prefixes must have <paramref name="Name"/> match <see cref="TypeUse.Name"/>
        /// </summary>
        /// <param name="typeUse">the type use to check</param>
        /// <returns>true if this alias may represent this type use.</returns>
        public bool IsAliasFor(TypeUse typeUse) {
            if(null == typeUse)
                throw new ArgumentNullException("typeUse");

            if(IsNamespaceAlias)
                return true;

            if(typeUse.Name == this.ImportedNamedScope.Name)
                return true;
            return false;
        }

        public string MakeQualifiedName(TypeUse typeUse) {
            throw new NotImplementedException();
        }
    }
}
