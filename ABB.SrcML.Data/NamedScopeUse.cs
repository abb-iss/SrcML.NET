using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The NamedScopeUse class represents a use of a named scope. It can create a <see cref="NamedScope"/> based
    /// on itself by calling <see cref="NamedScopeUse.CreateScope()"/>.
    /// </summary>
    public class NamedScopeUse : AbstractUse<NamedScope> {
        /// <summary>
        /// The child of this scope
        /// </summary>
        public NamedScopeUse ChildScopeUse { get; set; }

        /// <summary>
        /// Creates a <see cref="NamedScope"/> object from this use (along with all of its descendants based on <see cref="ChildScopeUse"/>).
        /// </summary>
        /// <returns>A new named scope based on this use</returns>
        public virtual NamedScope CreateScope() {
            var scope = new NamedScope() {
                Name = this.Name,
                ProgrammingLanguage = this.ProgrammingLanguage,
            };
            scope.AddSourceLocation(this.Location);
            if(null != this.ChildScopeUse) {
                scope.AddChildScope(ChildScopeUse.CreateScope());
            }
            return scope;
        }

        /// <summary>
        /// Constructs the full name for this named scope use by combining this scope with all of its <see cref="ChildScopeUse">children</see>
        /// </summary>
        /// <returns>The full name</returns>
        public string GetFullName() {
            StringBuilder sb = new StringBuilder();
            var current = this;
            while(current != null) {
                sb.Append(current.Name);
                current = current.ChildScopeUse;

                if(null != current) {
                    sb.Append('.');
                }
            }
            return sb.ToString();
        }
        /// <summary>
        /// Returns true if this scope matches <paramref name="definition"/>
        /// </summary>
        /// <param name="definition">The scope to check</param>
        /// <returns>True if this and definition have the same name</returns>
        public override bool Matches(NamedScope definition) {
            if(null == definition) return false;
            return definition.Name == this.Name;
        }
    }
}
