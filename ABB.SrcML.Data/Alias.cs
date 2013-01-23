using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class Alias {
        public string Name { get; set; }
        public string NamespaceName { get; set; }

        public bool IsNamespaceAlias { get { return Name.Length == 0; } }

        public Alias() {
            this.Name = String.Empty;
            this.NamespaceName = String.Empty;
        }
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
