using System;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents the use of a namespace. This is primarily used in <see cref="Alias"/> objects
    /// </summary>
    [Serializable]
    public class NamespaceUse : NamedScopeUse {

        /// <summary>
        /// Creates a <see cref="NamespaceDefinition"/> object from this use (along with all of its
        /// descendants based on <see cref="INamedScopeUse.ChildScopeUse"/>).
        /// </summary>
        /// <returns>A new namespace definition based on this use</returns>
        public override INamedScope CreateScope() {
            var ns = new NamespaceDefinition {
                Name = this.Name,
                ProgrammingLanguage = this.ProgrammingLanguage
            };
            ns.AddSourceLocation(this.Location);
            if(ChildScopeUse != null) {
                ns.AddChildScope(ChildScopeUse.CreateScope());
            }
            return ns;
        }
    }
}