using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class VariableScope {
        public Collection<VariableDeclaration> DeclaredVariables { get; set; }
        public string XPath { get; set; }

        public VariableScope() {
            DeclaredVariables = new Collection<VariableDeclaration>();
        }
        private static HashSet<XName> _containerNames = new HashSet<XName>(new XName[] {
            SRC.Block, SRC.Catch, SRC.Class, SRC.Constructor, SRC.Destructor,
            SRC.Do, SRC.Else, SRC.Enum, SRC.Extern, SRC.For, SRC.Function, SRC.If,
            SRC.Namespace, SRC.Private, SRC.Protected, SRC.Public, SRC.Struct, 
            SRC.Switch, SRC.Template, SRC.Then, SRC.Try, SRC.Typedef, SRC.Union,
            SRC.Unit, SRC.While
        });
        public static HashSet<XName> Containers {
            get { return _containerNames; }
        }
    }
}
