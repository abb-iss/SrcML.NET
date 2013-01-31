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
    public class VariableScope {
        protected Collection<VariableScope> ChildScopeCollection;
        protected Dictionary<string, VariableDeclaration> DeclaredVariablesDictionary;

        public VariableScope ParentScope { get; set; }
        public IEnumerable<VariableScope> ChildScopes { get { return this.ChildScopeCollection.AsEnumerable(); } }
        public IEnumerable<VariableDeclaration> DeclaredVariables { get { return this.DeclaredVariablesDictionary.Values.AsEnumerable(); } }

        public IEnumerable<VariableScope> ParentScopes {
            get {
                var current = this.ParentScope;
                while(null != current) {
                    yield return current;
                    current = current.ParentScope;
                }
            }
        }

        public string XPath { get; set; }

        public VariableScope() {
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>();
            ChildScopeCollection = new Collection<VariableScope>();
        }

        public void AddChildScope(VariableScope childScope) {
            ChildScopeCollection.Add(childScope);
            childScope.ParentScope = this;
        }

        public void AddDeclaredVariable(VariableDeclaration declaration) {
            DeclaredVariablesDictionary[declaration.Name] = declaration;
            declaration.Scope = this;
        }

        public bool IsScopeFor(XElement element) {
            return IsScopeFor(element.GetXPath(false));
        }

        public virtual bool IsScopeFor(string xpath) {
            return xpath.StartsWith(this.XPath);
        }

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
