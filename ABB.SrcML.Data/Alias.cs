using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents an import or using directive (usually found at the top of a source file)
    /// </summary>
    public class Alias {
        /// <summary>
        /// The name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///  the namespace name
        /// </summary>
        public string NamespaceName { get; set; }

        /// <summary>
        /// Returns true if this is a namespace alias (true if the name is not set).
        /// </summary>
        public bool IsNamespaceAlias { get { return Name.Length == 0; } }

        /// <summary>
        /// Constructs a new alias object
        /// </summary>
        public Alias() {
            this.Name = String.Empty;
            this.NamespaceName = String.Empty;
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

            if(typeUse.Name == this.Name)
                return true;
            return false;
        }

        public string MakeQualifiedName(TypeUse typeUse) {
            throw new NotImplementedException();
        }
    }
}
