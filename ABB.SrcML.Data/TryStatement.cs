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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class TryStatement : BlockStatement {
        private List<CatchStatement> catchStatementsList;
        private List<Statement> finallyStatementsList;

        public TryStatement() : base() {
            catchStatementsList = new List<CatchStatement>();
            CatchStatements = new ReadOnlyCollection<CatchStatement>(catchStatementsList);
            finallyStatementsList = new List<Statement>();
            FinallyStatements = new ReadOnlyCollection<Statement>(finallyStatementsList);
        }

        public ReadOnlyCollection<CatchStatement> CatchStatements { get; private set; }
        public ReadOnlyCollection<Statement> FinallyStatements { get; private set; }

        public void AddCatchStatement(CatchStatement catchStmt) {
            if(catchStmt == null) { throw new ArgumentNullException("catchStmt"); }
            catchStmt.ParentStatement = this;
            catchStatementsList.Add(catchStmt);
        }

        public void AddCatchStatements(IEnumerable<CatchStatement> catchStmts) {
            foreach(var stmt in catchStmts) {
                AddCatchStatement(stmt);
            }
        }

        public void AddFinallyStatement(Statement finallyStmt) {
            if(finallyStmt == null) { throw new ArgumentNullException("finallyStmt"); }
            finallyStmt.ParentStatement = this;
            finallyStatementsList.Add(finallyStmt);
        }

        public void AddFinallyStatements(IEnumerable<Statement> finallyStmts) {
            foreach(var stmt in finallyStmts) {
                AddFinallyStatement(stmt);
            }
        }
    }
}
