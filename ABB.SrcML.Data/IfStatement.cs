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
        /// If <paramref name="child"/> is null, nothing will be done.
        /// </summary>
        /// <param name="child">The statement to add.</param>
        public void AddElseStatement(Statement child) {
            if(child != null) {
                child.ParentStatement = this;
                elseStatementsList.Add(child);
            }
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

        /// <summary>
        /// Returns the child statements, including those in the Else block.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return base.GetChildren().Concat(ElseStatements);
        }

        /// <summary> Returns the XML name for this program element. </summary>
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
            if(ElseStatements.Count > 0) {
                var firstElseLoc = ElseStatements.First().PrimaryLocation;
                var lastElseLoc = ElseStatements.Last().PrimaryLocation;
                var elseLocation = new SourceLocation(firstElseLoc.SourceFileName, firstElseLoc.StartingLineNumber, firstElseLoc.StartingColumnNumber, lastElseLoc.EndingLineNumber, lastElseLoc.EndingColumnNumber);
                if(string.Compare(elseLocation.SourceFileName, use.Location.SourceFileName, StringComparison.OrdinalIgnoreCase) == 0
                   && elseLocation.Contains(use.Location)) {
                    //the use is in the else-block, don't return results from the then-block
                    return matches.SkipWhile(m => PositionComparer.CompareLocation(m.GetLocations().First(), elseLocation) < 0);
                }
            }
            return matches;
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("if({0})", Condition);
        }
    }
}
