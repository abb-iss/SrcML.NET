using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents an import or using directive (usually found at the top of a source file)
    /// </summary>
    public class Alias {
        private NamespaceUse namespaceRoot;
        private NamedScopeUse endPoint;

        /// <summary>
        /// The namespace root identified by this alias
        /// </summary>
        public NamespaceUse ImportedNamespace {
            get { return namespaceRoot; }
            set { this.namespaceRoot = value; }
        }

        /// <summary>
        /// the specific object identified by this alias
        /// </summary>
        public NamedScopeUse ImportedNamedScope {
            get { return endPoint; }
            set {
                this.endPoint = value;
                
            }
        }

        /// <summary>
        /// Returns true if this is a namespace alias (true if the name is not set).
        /// </summary>
        public bool IsNamespaceImport { get { return ImportedNamedScope == null; } }

        /// <summary>
        /// The location of this alias in both the source file and the XML.
        /// </summary>
        public SrcMLLocation Location { get; set; }

        /// <summary>
        /// The programming language this alias was produced from
        /// </summary>
        public Language ProgrammingLanguage { get; set; }

        
        /// <summary>
        /// Constructs a new alias object
        /// </summary>
        public Alias() {
            this.ImportedNamedScope = null;
            this.ImportedNamespace = null;
        }

        /// <summary>
        /// Constructs the namespace name for this alias
        /// </summary>
        /// <returns>the namespace name</returns>
        public string GetNamespaceName() {
            if(null == this.ImportedNamespace)
                return String.Empty;
            return this.ImportedNamespace.GetFullName();
        }

        /// <summary>
        /// Gets the full name for this alias
        /// </summary>
        /// <returns>the full name for this alias</returns>
        public string GetFullName() {
            return String.Format("{0}.{1}", GetNamespaceName(), ImportedNamedScope.GetFullName()).TrimStart('.');
        }
        /// <summary>
        /// Checks if this is a valid alias for the given type use. Namespace prefixes are always valid.
        /// Other prefixes must have <see cref="ImportedNamedScope"/> match <see cref="AbstractUse{T}.Name"/>
        /// </summary>
        /// <param name="typeUse">the type use to check</param>
        /// <returns>true if this alias may represent this type use.</returns>
        public bool IsAliasFor(TypeUse typeUse) {
            if(null == typeUse)
                throw new ArgumentNullException("typeUse");

            if(IsNamespaceImport)
                return true;

            if(typeUse.Name == this.ImportedNamedScope.Name)
                return true;
            return false;
        }

        /// <summary>
        /// Checks to see if this is an alias for <paramref name="namedScope"/>
        /// </summary>
        /// <param name="namedScope">The named scope to check</param>
        /// <returns>True if this alias can apply to the provided named scope; false otherwise</returns>
        public bool IsAliasFor(NamedScope namedScope) {
            if(this.IsNamespaceImport)
                return true;
            return false;
        }
    }
}
