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
        /// If <paramref name="catchStmt"/> is null, nothing will be done.
        /// </summary>
        /// <param name="catchStmt">The catch statement to add.</param>
        public void AddCatchStatement(CatchStatement catchStmt) {
            if(catchStmt != null) {
                catchStmt.ParentStatement = this;
                catchStatementsList.Add(catchStmt);
            }
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
        /// If <paramref name="finallyStmt"/> is null, nothing will be done.
        /// </summary>
        /// <param name="finallyStmt">The statement to add.</param>
        public void AddFinallyStatement(Statement finallyStmt) {
            if(finallyStmt != null) {
                finallyStmt.ParentStatement = this;
                finallyStatementsList.Add(finallyStmt);
            }
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
        /// Returns the child statements, including the catch and finally statements.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return base.GetChildren().Concat(CatchStatements).Concat(FinallyStatements);
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

        /// <summary>
        /// Returns the children of this statement that have the same name as the given <paramref name="use"/>, and the given type.
        /// This method searches only the immediate children, and not further descendants.
        /// If the <paramref name="use"/> occurs within this statement, this method will return only the children
        /// that occur prior to that use.
        /// </summary>
        /// <typeparam name="T">The type of children to return.</typeparam>
        /// <param name="use">The use containing the name to search for.</param>
        /// <param name="searchDeclarations">Whether to search the child DeclarationStatements for named entities.</param>
        public override IEnumerable<T> GetNamedChildren<T>(NameUse use, bool searchDeclarations) {
            var matches = base.GetNamedChildren<T>(use, searchDeclarations);
            //check if we should filter the results
            if(ChildStatements.Count > 0) {
                var firstTryLoc = ChildStatements.First().PrimaryLocation;
                var lastTryLoc = ChildStatements.Last().PrimaryLocation;
                var tryBlockLocation = new SourceLocation(firstTryLoc.SourceFileName, firstTryLoc.StartingLineNumber, firstTryLoc.StartingColumnNumber, lastTryLoc.EndingLineNumber, lastTryLoc.EndingColumnNumber);
                if(this.PrimaryLocation.Contains(use.Location) && !tryBlockLocation.Contains(use.Location)) {
                    //the use is within the overall TryStatement, but not in the try block. Don't return results from the try block
                    return matches.SkipWhile(m => tryBlockLocation.Contains(m.GetLocations().First()));
                }
            }
            return matches;
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return "try";
        }
    }
}
