/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

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
