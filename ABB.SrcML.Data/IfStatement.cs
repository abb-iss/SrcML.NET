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
    public class IfStatement : ConditionBlockStatement {
        private List<Statement> elseStatementsList;
        
        public IfStatement() : base() {
            elseStatementsList = new List<Statement>();
            ElseStatements = new ReadOnlyCollection<Statement>(elseStatementsList);
        }
        
        public ReadOnlyCollection<Statement> ElseStatements { get; private set; }
        public void AddElseStatement(Statement child) {
            if(child == null) { throw new ArgumentNullException("child"); }

            child.ParentStatement = this;
            elseStatementsList.Add(child);
        }
    }
}
