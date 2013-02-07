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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The VariableScope class is the base class for variable scope objects. It encapsulates the basics of the type hierarchy (parent-child relationships)
    /// and contains methods for adding child scopes and variable declarations.
    /// </summary>
    public class VariableScope {
        /// <summary>
        /// Holds all of the children for this scope.
        /// </summary>
        protected Collection<VariableScope> ChildScopeCollection;

        /// <summary>
        /// Holds all of the variable declarations declared here. The key is the variable name.
        /// </summary>
        protected Dictionary<string, VariableDeclaration> DeclaredVariablesDictionary;

        /// <summary>
        /// The parent container for this scope.
        /// </summary>
        public VariableScope ParentScope { get; set; }

        /// <summary>
        /// Iterates over all of the child scopes of this class
        /// </summary>
        public IEnumerable<VariableScope> ChildScopes { get { return this.ChildScopeCollection.AsEnumerable(); } }

        /// <summary>
        /// Iterates over all of the variable declarations for this scope
        /// </summary>
        public IEnumerable<VariableDeclaration> DeclaredVariables { get { return this.DeclaredVariablesDictionary.Values.AsEnumerable(); } }

        /// <summary>
        /// The parent scopes for this scope in reverse order (parent is returned first, followed by the grandparent, etc).
        /// </summary>
        public IEnumerable<VariableScope> ParentScopes {
            get {
                var current = this.ParentScope;
                while(null != current) {
                    yield return current;
                    current = current.ParentScope;
                }
            }
        }

        /// <summary>
        /// The namespace name for this scope. The namespace name is taken by iterating over all of the parents and selecting only the namespace definitions.
        /// </summary>
        public string NamespaceName {
            get {
                var namespaceParents = from p in this.ParentScopes
                                       let ns = (p as NamespaceDefinition)
                                       where !(null == ns || ns.IsGlobal)
                                       select ns.Name;
                return String.Join(".", namespaceParents.Reverse());
            }
        }

        /// <summary>
        /// The XPath query where this scope is located.
        /// </summary>
        public string XPath { get; set; }

        /// <summary>
        /// Initializes an empty variable scope.
        /// </summary>
        public VariableScope() {
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>();
            ChildScopeCollection = new Collection<VariableScope>();
        }

        /// <summary>
        /// Adds a child scope to this scope
        /// </summary>
        /// <param name="childScope">The child scope to add.</param>
        public virtual void AddChildScope(VariableScope childScope) {
            int i;
            VariableScope mergedScope = null;
            
            for(i = 0; i < this.ChildScopeCollection.Count; i++) {
                mergedScope = this.ChildScopeCollection.ElementAt(i).Merge(childScope);
                if(null != mergedScope) {
                    break;
                }
            }

            if(null == mergedScope) {
                ChildScopeCollection.Add(childScope);
                childScope.ParentScope = this;
            } else if(mergedScope == childScope) {
                ChildScopeCollection[i] = mergedScope;
                mergedScope.ParentScope = this;
            }
        }

        /// <summary>
        /// Add a variable declaration to this scope
        /// </summary>
        /// <param name="declaration">The variable declaration to add.</param>
        public void AddDeclaredVariable(VariableDeclaration declaration) {
            DeclaredVariablesDictionary[declaration.Name] = declaration;
            declaration.Scope = this;
        }

        /// <summary>
        /// The merge function merges two scopes if they are the same. It assumes that the parents of the two scopes are identical.
        /// Because of this, it is best to call it on two "global scope" objects. If the two scopes are the same (as determined by
        /// the <see cref="CanBeMergedWith"/> method), then the variable declarations in <paramref name="otherScope"/> are added to this scope
        /// and the child scopes of <paramref name="otherScope"/> are merged with the child scopes of this scope.
        /// </summary>
        /// <param name="otherScope">The scope to merge with.</param>
        /// <returns>True if the scopes were merged, false otherwise.</returns>
        public virtual VariableScope Merge(VariableScope otherScope) {
            if(CanBeMergedWith(otherScope)) {
                return AddFrom(otherScope);
            }
            return null;
        }

        /// <summary>
        /// The AddFrom function adds all of the declarations and children from <paramref name="otherScope"/> to this scope
        /// </summary>
        /// <param name="otherScope">The scope to add data from</param>
        /// <returns>the new scope</returns>
        public VariableScope AddFrom(VariableScope otherScope) {
            foreach(var declaration in otherScope.DeclaredVariables) {
                this.AddDeclaredVariable(declaration);
            }

            foreach(var newChild in otherScope.ChildScopes) {
                AddChildScope(newChild);
            }
            return this;
        }
        /// <summary>
        /// Tests value equality between this scope and <paramref name="otherScope"/>.
        /// Two scopes are equal if they have the same <see cref="VariableScope.XPath"/>.
        /// </summary>
        /// <param name="otherScope">The scope to compare to</param>
        /// <returns>True if the scopes are the same. False otherwise.</returns>
        public virtual bool CanBeMergedWith(VariableScope otherScope) {
            return (null != otherScope && this.XPath == otherScope.XPath);
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XElement. A variable scope contains an element if <see cref="VariableScope.XPath"/> is a 
        /// prefix for the XPath for <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to look for</param>
        /// <returns>true if this is a container for <paramref name="element"/>. False otherwise.</returns>
        public bool IsScopeFor(XElement element) {
            return IsScopeFor(element.GetXPath(false));
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XPath. A variable scope contains an expath if <see cref="VariableScope.XPath"/> is a prefix for <paramref name="xpath"/>
        /// </summary>
        /// <param name="xpath">The xpath to look for.</param>
        /// <returns>True if this is a container for the given xpath. False, otherwise.</returns>
        public virtual bool IsScopeFor(string xpath) {
            return xpath.StartsWith(this.XPath);
        }

        /// <summary>
        /// returns an enumerable of all the scopes rooted here that are containers for this XPath. Includes the current scope.
        /// </summary>
        /// <param name="xpath">the xpath to find containers for.</param>
        /// <returns>an enumerable of all the scopes rooted here that are containers for this XPath. Includes the current scope.</returns>
        public IEnumerable<VariableScope> GetScopesForPath(string xpath) {
            if(IsScopeFor(xpath)) {
                yield return this;

                foreach(var child in this.ChildScopes) {
                    foreach(var matchingScope in child.GetScopesForPath(xpath)) {
                        yield return matchingScope;
                    }
                }
            }
        }

        /// <summary>
        /// Searches this scope and all of its children for variable declarations that match the given variable name and xpath.
        /// It finds all the scopes where the xpath is valid and all of the declarations in those scopes that match the variable name.
        /// </summary>
        /// <param name="variableName">the variable name to search for.</param>
        /// <param name="xpath">the xpath for the variable name</param>
        /// <returns>An enumerable of matching variable declarations.</returns>
        public IEnumerable<VariableDeclaration> GetDeclarationsForVariableName(string variableName, string xpath) {
            foreach(var scope in GetScopesForPath(xpath)) {
                VariableDeclaration declaration;
                if(scope.DeclaredVariablesDictionary.TryGetValue(variableName, out declaration)) {
                    yield return declaration;
                }
            }
        }
    }
}
