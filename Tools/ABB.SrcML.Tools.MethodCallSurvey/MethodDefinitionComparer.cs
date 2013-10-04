using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    internal class MethodDefinitionComparer : IEqualityComparer<MethodDefinition> {

        public bool Equals(MethodDefinition x, MethodDefinition y) {
            return x.Id.Equals(y.Id, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(MethodDefinition obj) {
            return obj.Id.GetHashCode();
        }
    }
}