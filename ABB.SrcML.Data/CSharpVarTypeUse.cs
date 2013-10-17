using System.Collections.Generic;

namespace ABB.SrcML.Data {

    public class CSharpVarTypeUse : TypeUse {

        public IResolvesToType Initializer { get; set; }

        public override IEnumerable<TypeDefinition> FindMatches() {
            if(Initializer != null) {
                return Initializer.FindMatchingTypes();
            }
            return base.FindMatches();
        }
    }
}