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
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents an if-statement in a program.
    /// </summary>
    public class IfStatement : ConditionBlockStatement {
        private List<Statement> elseStatementsList;
        
        /// <summary> The XML name for IfStatement. </summary>
        public new const string XmlName = "If";

        /// <summary> The XML name for <see cref="ElseStatements"/>. </summary>
        public const string XmlElseName = "Else";

        /// <summary> Creates a new empty IfStatement. </summary>
        public IfStatement() : base() {
            elseStatementsList = new List<Statement>();
            ElseStatements = new ReadOnlyCollection<Statement>(elseStatementsList);
        }
        
        /// <summary>
        /// The statements contained within the else block, if any.
        /// </summary>
        public ReadOnlyCollection<Statement> ElseStatements { get; private set; }
        
        /// <summary>
        /// Adds the given statement to the ElseStatements collection.
        /// </summary>
        /// <param name="child">The statement to add.</param>
        public void AddElseStatement(Statement child) {
            if(child == null) { throw new ArgumentNullException("child"); }

            child.ParentStatement = this;
            elseStatementsList.Add(child);
        }

        /// <summary>
        /// Adds the given statements to the ElseStatements collection.
        /// </summary>
        /// <param name="elseStatements">An enumerable of statements to add.</param>
        public void AddElseStatements(IEnumerable<Statement> elseStatements) {
            foreach(var stmt in elseStatements) {
                AddElseStatement(stmt);
            }
        }

        public override string GetXmlName() { return IfStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlElseName == reader.Name) {
                AddElseStatements(XmlSerialization.ReadChildStatements(reader));
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            base.WriteXmlContents(writer);
            XmlSerialization.WriteCollection<Statement>(writer, XmlElseName, ElseStatements);
        }
    }
}
