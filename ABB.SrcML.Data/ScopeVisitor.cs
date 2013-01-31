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
    /// The ScopeVisitor class implements the Visitor pattern as found in the Design Patterns book.
    /// It takes a SrcML XElement as input to the Visit method and then generates an IEnumerable of <see cref="VariableScope"/> objects.
    /// <see cref="Parser"/> determines how the scope objects are created, and what the children of each XElement are.
    /// </summary>
    public class ScopeVisitor {
        AbstractCodeParser Parser;

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
        public ScopeVisitor(AbstractCodeParser parser) {
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
        public IEnumerable<VariableScope> Visit(XElement element) {
            var scopeForElement = CreateScope(element);
            if(ScopeStack.Count > 0) {
                CurrentScope.AddChildScope(scopeForElement);
            }
            ScopeStack.Push(scopeForElement);

            foreach(var variable in Parser.GetVariableDeclarationsFromContainer(element, FileUnit)) {
                CurrentScope.AddDeclaredVariable(variable);
            }

            foreach(var child in Parser.GetChildContainers(element)) {
                foreach(var childScope in Visit(child)) {
                    yield return childScope;
                }
            }

            yield return ScopeStack.Pop();
        }

        /// <summary>
        /// Looks at the name of the element and then creates a variablescope depending on the <see cref="System.Xml.Linq.XName"/>.
        /// </summary>
        /// <param name="element">The element to create a scope for</param>
        /// <returns>A variable scope for the element</returns>
        private VariableScope CreateScope(XElement element) {
            VariableScope scope;
            if(element.Name == SRC.Unit) {
                FileUnit = element;
                scope = Parser.CreateScopeFromFile(element);
            } else if(Parser.TypeElementNames.Contains(element.Name)) {
                scope = Parser.CreateTypeDefinition(element, FileUnit);
            } else if(Parser.NamespaceElementNames.Contains(element.Name)) {
                scope = Parser.CreateNamespaceDefinition(element, FileUnit);
            } else if(Parser.MethodElementNames.Contains(element.Name)) {
                scope = Parser.CreateMethodDefinition(element, FileUnit);
            } else {
                scope = Parser.CreateScopeFromContainer(element, FileUnit);
            }
            scope.XPath = element.GetXPath(false);
            return scope;
        }
    }
}
