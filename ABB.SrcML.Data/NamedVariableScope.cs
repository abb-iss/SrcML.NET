using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class NamedVariableScope : VariableScope {
        public string Name { get; set; }

        public NamedVariableScope()
            : base() {
                Name = String.Empty;
        }
    }
}
