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
        /// Finds a namespace that matches the <see cref="ImportedNamespace"/> portion of this alias.
        /// </summary>
        /// <param name="rootScope">the global scope to search from</param>
        /// <returns>namespace definitions rooted at <paramref name="rootScope"/> that match <see cref="ImportedNamespace"/></returns>
        public IEnumerable<NamespaceDefinition> FindMatchingNamespace(NamespaceDefinition rootScope) {
            var currentNsUse = this.ImportedNamespace;

            List<NamespaceDefinition> scopes = new List<NamespaceDefinition>();
            scopes.Add(rootScope);

            // we will go through each namespace referenced by the alias
            while(currentNsUse != null) {
                // go through all of the scopes and get the children that match currentNsUse
                // on the first iteration, the only thing in scopes will be the global scope
                // on subsequent iterations, scopes will contain matches for the parent of currentNsUse
                int currentLength = scopes.Count;
                for(int i = 0; i < currentLength; i++) {
                    scopes.AddRange(scopes[i].GetChildScopesWithId<NamespaceDefinition>(currentNsUse.Name));
                }
                // once we've found matches for currentNsUse, remove the previous scopes from the list
                // and set currentNsUse to its child
                scopes.RemoveRange(0, currentLength);
                currentNsUse = currentNsUse.ChildScopeUse as NamespaceUse;
            }

            return scopes;
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
        /// <param name="use">the type use to check</param>
        /// <returns>true if this alias may represent this type use.</returns>
        public bool IsAliasFor<DEFINITION>(AbstractUse<DEFINITION> use) where DEFINITION : class {
            if(null == use)
                throw new ArgumentNullException("use");

            if(IsNamespaceImport)
                return true;

            if(use.Name == this.ImportedNamedScope.Name)
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
