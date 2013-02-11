/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The SrcMLElementVisitor class implements the Visitor pattern as found in the Design Patterns book.
    /// It takes a SrcML XElement as input to the Visit method and then generates an IEnumerable of <see cref="VariableScope"/> objects.
    /// <see cref="Parser"/> determines how the scope objects are created, and what the children of each XElement are.
    /// </summary>
    public class SrcMLElementVisitor {
        AbstractCodeParser Parser;

        /// <summary>
        /// The current file unit being parsed. It is set when a <see cref="ABB.SrcML.SRC.Unit"/> is visited.
        /// </summary>
        public XElement FileUnit;
        /// <summary>
        /// Returns the most recent scope from <see cref="ScopeStack"/>.
        /// </summary>
        public VariableScope CurrentScope { get { return ScopeStack.Peek(); } }

        /// <summary>
        /// A stack of scopes
        /// </summary>
        public Stack<VariableScope> ScopeStack;
        
        /// <summary>
        /// Creates a new scope visitor
        /// </summary>
        /// <param name="parser">The language-specific parser to be used.</param>
        public SrcMLElementVisitor(AbstractCodeParser parser) {
            this.Parser = parser;
            this.ScopeStack = new Stack<VariableScope>();
            
        }

        /// <summary>
        /// <para>Visit constructs a scope element a VariableScope and then recursively visits all of the children of the element.</para>
        /// 
        /// <para>It relies on <see cref="Parser"/> to determine construct the VariableScope and determine what the children of the element are.</para>
        /// </summary>
        /// <param name="element">The element to visit</param>
        /// <returns>All of the VariableScope objects rooted at this element.</returns>
        public VariableScope Visit(XElement element) {
            ScopeStack.Push(CreateScope(element));

            foreach(var variable in Parser.GetVariableDeclarationsFromContainer(element, FileUnit, this.CurrentScope)) {
                CurrentScope.AddDeclaredVariable(variable);
            }

            foreach(var child in Parser.GetChildContainers(element)) {
                var childScope = Visit(child);
                CurrentScope.AddChildScope(childScope);
            }

            return ScopeStack.Pop();
        }

        /// <summary>
        /// Looks at the name of the element and then creates a variablescope depending on the <see cref="System.Xml.Linq.XName"/>.
        /// </summary>
        /// <param name="element">The element to create a scope for</param>
        /// <returns>A variable scope for the element</returns>
        private VariableScope CreateScope(XElement element) {
            VariableScope scope = Parser.CreateScope(element, FileUnit);

            if(element.Name == SRC.Unit) {
                FileUnit = element;
            }
            return scope;
        }
        /// <summary>
        /// Convenience method for constructing the visitor and creating the root scope object for an XElement.
        /// </summary>
        /// <param name="element">The element to start construction at</param>
        /// <param name="parser">The language-specific parser to use</param>
        /// <returns>The scope object for the element</returns>
        public static VariableScope Visit(XElement element, AbstractCodeParser parser) {
            return (new SrcMLElementVisitor(parser)).Visit(element);
        }
    }
}
