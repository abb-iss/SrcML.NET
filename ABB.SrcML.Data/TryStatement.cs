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
    /// Represents a try block in a program.
    /// </summary>
    public class TryStatement : BlockStatement {
        private List<CatchStatement> catchStatementsList;
        private List<Statement> finallyStatementsList;

        /// <summary> The XML name for TryStatement </summary>
        public new const string XmlName = "Try";

        /// <summary> XML Name for <see cref="CatchStatements" /> </summary>
        public const string XmlCatchName = "Catch";

        /// <summary> XML Name for <see cref="FinallyStatements" /> </summary>
        public const string XmlFinallyName = "Finally";

        /// <summary> Creates a new empty TryStatement. </summary>
        public TryStatement() : base() {
            catchStatementsList = new List<CatchStatement>();
            CatchStatements = new ReadOnlyCollection<CatchStatement>(catchStatementsList);
            finallyStatementsList = new List<Statement>();
            FinallyStatements = new ReadOnlyCollection<Statement>(finallyStatementsList);
        }

        /// <summary> The catch statements associated with this try, if any. </summary>
        public ReadOnlyCollection<CatchStatement> CatchStatements { get; private set; }

        /// <summary> The contents of the finally block associated with this try, if any. </summary>
        public ReadOnlyCollection<Statement> FinallyStatements { get; private set; }

        /// <summary>
        /// Instance method for getting <see cref="TryStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for TryStatement</returns>
        public override string GetXmlName() { return TryStatement.XmlName; }

        /// <summary>
        /// Adds the given catch to the CatchStatements collection.
        /// </summary>
        /// <param name="catchStmt">The catch statement to add.</param>
        public void AddCatchStatement(CatchStatement catchStmt) {
            if(catchStmt == null) { throw new ArgumentNullException("catchStmt"); }
            catchStmt.ParentStatement = this;
            catchStatementsList.Add(catchStmt);
        }

        /// <summary>
        /// Adds the given catches to the CatchStatements collection.
        /// </summary>
        /// <param name="catchStmts">An enumerable of catch statements to add.</param>
        public void AddCatchStatements(IEnumerable<CatchStatement> catchStmts) {
            foreach(var stmt in catchStmts) {
                AddCatchStatement(stmt);
            }
        }

        /// <summary>
        /// Adds the given statement to the FinallyStatements collection.
        /// </summary>
        /// <param name="finallyStmt">The statement to add.</param>
        public void AddFinallyStatement(Statement finallyStmt) {
            if(finallyStmt == null) { throw new ArgumentNullException("finallyStmt"); }
            finallyStmt.ParentStatement = this;
            finallyStatementsList.Add(finallyStmt);
        }

        /// <summary>
        /// Adds the given statements to the FinallyStatements collection.
        /// </summary>
        /// <param name="finallyStmts">An enumerable of statements to add.</param>
        public void AddFinallyStatements(IEnumerable<Statement> finallyStmts) {
            foreach(var stmt in finallyStmts) {
                AddFinallyStatement(stmt);
            }
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlCatchName == reader.Name) {
                AddCatchStatements(XmlSerialization.ReadChildStatements(reader).Cast<CatchStatement>());
            } else if(XmlFinallyName == reader.Name) {
                AddFinallyStatements(XmlSerialization.ReadChildStatements(reader));
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
            if(null != CatchStatements) {
                XmlSerialization.WriteCollection<CatchStatement>(writer, XmlCatchName, CatchStatements);
            }
            if(null != FinallyStatements) {
                XmlSerialization.WriteCollection<Statement>(writer, XmlFinallyName, FinallyStatements);
            }
        }
    }
}
