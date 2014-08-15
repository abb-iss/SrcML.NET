using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    internal class StatementDebugView {
        private Statement statement;

        public StatementDebugView(Statement statement) {
            this.statement = statement;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public Statement[] ChildStatements {
            get { return this.statement.ChildStatements.ToArray(); }
        }

        public Expression Content {
            get { return this.statement.Content; }
        }

        public override string ToString() {
            return statement.ToString();
        }
    }
}
