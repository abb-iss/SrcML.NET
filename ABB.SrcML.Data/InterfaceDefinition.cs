using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    [DebuggerTypeProxy(typeof(StatementDebugView))]
    public class InterfaceDefinition : NamedScope {
        /// <summary> The XML name for InterfaceDefinition. </summary>
        public new const string XmlName = "Interface";
    }
}
