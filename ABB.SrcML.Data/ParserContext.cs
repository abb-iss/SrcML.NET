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
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Parser context objects store the current state of the
    /// <see cref="AbstractCodeParser.ParseStatement"/> method.
    /// </summary>
    public class ParserContext {
        private XElement fileUnitBeingParsed;

        /// <summary>
        /// Creates a new parser context
        /// </summary>
        public ParserContext()
            : this(null) {
        }

        /// <summary>
        /// Creates a new parser context
        /// </summary>
        /// <param name="fileUnit">The file unit for this context</param>
        public ParserContext(XElement fileUnit) {
            this.FileUnit = fileUnit;
            //ParentStatementStack = new Stack<Statement>();
            //StatementStack = new Stack<Statement>();
        }

        ///// <summary>
        ///// The aliases for this context. This should be set by a call to
        ///// <see cref="AbstractCodeParser.ParseUnitElement"/>.
        ///// </summary>
        //public Collection<Alias> Aliases { get; set; }

        ///// <summary>
        ///// The current statement on <see cref="ParentStatementStack"/>. If the stack is empty, it returns
        ///// null.
        ///// </summary>
        //public Statement CurrentParentStatement {
        //    get {
        //        if(ParentStatementStack.Count > 0)
        //            return ParentStatementStack.Peek();
        //        return null;
        //    }
        //}

        ///// <summary>
        ///// The current statement on <see cref="StatementStack"/>. If the stack is empty, it returns null.
        ///// </summary>
        //public Statement CurrentStatement {
        //    get {
        //        if(StatementStack.Count > 0)
        //            return StatementStack.Peek();
        //        return null;
        //    }
        //}

        /// <summary>
        /// The file name from <see cref="FileUnit"/>
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// The file unit for this context. This should be set by a call to
        /// <see cref="AbstractCodeParser.ParseUnitElement"/>. Alternatively, this can be set
        /// manually for calls to other Parse methods in <see cref="AbstractCodeParser"/>.
        /// </summary>
        public XElement FileUnit {
            get { return this.fileUnitBeingParsed; }
            set {
                if(null != value) {
                    if(value.Name != SRC.Unit)
                        throw new ArgumentException("must be a SRC.Unit", "value");
                    this.FileName = SrcMLElement.GetFileNameForUnit(value);
                } else {
                    this.FileName = string.Empty;
                }
                this.fileUnitBeingParsed = value;
            }
        }

        ///// <summary>
        ///// The parent statement stack stores the parent of the statement being parsed. This is only used in
        ///// specific cases such as the following C# example: <code language="C#"> namespace A.B.C {
        ///// } </code> In this example, we want the tree to be <c>A->B->C</c>. What
        ///// <see cref="AbstractCodeParser.ParseNamespaceElement(XElement,ParserContext)"/> does in
        ///// this case is create three namespaces: <c>A</c>, <c>B</c>, and <c>C</c> and puts them all
        ///// on <see cref="StatementStack"/>. Because we have created three elements, we need a way to
        ///// track how many need to be popped off. The <c>A</c> namespace will be put on
        ///// <see cref="ParentStatementStack"/>.
        ///// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> will see that
        ///// ParentStatementStack and <see cref="StatementStack"/> are not equal and it will
        ///// <see cref="System.Collections.Stack.Pop()"/> elements off until they are.
        ///// </summary>
        //private Stack<Statement> ParentStatementStack { get; set; }

        ///// <summary>
        ///// The statement stack stores all of the statements currently being parsed. When
        ///// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> creates a statement it
        ///// pushes it onto the stack. Once it has finished creating the statement (including calling
        ///// <see cref="AbstractCodeParser.ParseElement(XElement,ParserContext)"/> on all of its
        ///// children), it removes it from the stack.
        ///// </summary>
        //private Stack<Statement> StatementStack { get; set; }

        /// <summary>
        /// Creates a location object for the given
        /// <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to create a location for</param>
        /// <param name="isReference">whether or not this is a reference location</param>
        /// <returns>The new location object. The <see cref="SourceLocation.SourceFileName"/> will
        /// be set to see cref="FileName"/></returns>
        public SrcMLLocation CreateLocation(XElement element, bool isReference) {
            var location = new SrcMLLocation(element, this.FileName, isReference);
            return location;
        }

        /// <summary>
        /// Creates a location object for the given
        /// <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to create a location for</param>
        /// <returns>The new location object. The <see cref="SourceLocation.SourceFileName"/> will
        /// be set to see cref="FileName"/></returns>
        public SrcMLLocation CreateLocation(XElement element) {
            var location = new SrcMLLocation(element, this.FileName);
            return location;
        }

        ///// <summary>
        ///// Removes the most recent statement from the statement stack and returns it. If intermediate
        ///// statements were inserted, it calls <see cref="RevertToNextParent()"/>.
        ///// </summary>
        ///// <returns>The most recent statement.</returns>
        //public Statement Pop() {
        //    RevertToNextParent();
        //    ParentStatementStack.Pop();
        //    return StatementStack.Pop();
        //}

        ///// <summary>
        ///// Adds
        ///// <paramref name="statement"/>to the statement stack. This simply calls
        ///// <see cref="Push(Statement,Statement)"/> with both arguments set to
        ///// <paramref name="statement"/></summary>
        ///// <param name="statement">The statement to add.</param>
        //public void Push(Statement statement) {
        //    Push(statement, statement);
        //}

        ///// <summary>
        ///// Adds
        ///// <paramref name="statement"/>and
        ///// <paramref name="parent">it's parent</paramref> . If <see cref="CurrentParentStatement"/> is
        ///// equal to
        ///// <paramref name="parent"/>then parent is not added.
        ///// </summary>
        ///// <param name="statement"></param>
        ///// <param name="parent"></param>
        //public void Push(Statement statement, Statement parent) {
        //    StatementStack.Push(statement);
        //    if(parent != CurrentParentStatement) {
        //        ParentStatementStack.Push(parent);
        //    }
        //}

        ///// <summary>
        ///// Removes statements until <c>CurrentStatement == CurrentParentStatement</c>. As each statement is
        ///// removed, it is added as a child to its predecessor.
        ///// </summary>
        //public void RevertToNextParent() {
        //    while(CurrentStatement != CurrentParentStatement) {
        //        var statement = StatementStack.Pop();
        //        CurrentStatement.AddChildScope(statement);
        //    }
        //}
    }
}