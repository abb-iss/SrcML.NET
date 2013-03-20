using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    internal class ScopeDebugView {
        private Scope scope;

        public ScopeDebugView(Scope scope) {
            this.scope = scope;
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Scope[] ChildScopes { get { return this.scope.ChildScopes.ToArray(); }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public MethodCall[] MethodCalls { get { return this.scope.MethodCalls.ToArray(); } }

        [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
        public VariableDeclaration[] Variables { get { return this.scope.DeclaredVariables.ToArray(); } }

        public override string ToString() {
            return scope.ToString();
        }
    }
}
