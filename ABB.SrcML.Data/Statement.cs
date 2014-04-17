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
    public class Statement {
        private List<Statement> childStatementsList;
        
        public Statement() {
            childStatementsList = new List<Statement>();
            ChildStatements = new ReadOnlyCollection<Statement>(childStatementsList);
        }
        
        public ReadOnlyCollection<Statement> ChildStatements { get; private set; }
        public Statement ParentStatement { get; set; }
        public SrcMLLocation Location { get; set; }
        public Language ProgrammingLanguage { get; set; }
        public Expression Content {get; set;}

        public void AddChildStatement(Statement child) {
            if(child == null) { throw new ArgumentNullException("child"); }

            child.ParentStatement = this;
            childStatementsList.Add(child);
        }

        public void AddChildStatements(IEnumerable<Statement> children) {
            foreach(var child in children) {
                AddChildStatement(child);
            }
        }

    }
}
