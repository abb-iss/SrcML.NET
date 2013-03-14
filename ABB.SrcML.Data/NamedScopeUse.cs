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

        public override bool Matches(NamedScope definition) {
            if(null == definition) return false;
            return definition.Name == this.Name;
        }
    }
}
