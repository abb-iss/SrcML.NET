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

namespace ABB.SrcML.Data {
    public class NamedVariableScope : VariableScope {
        public Dictionary<string, VariableScope> UnresolvedChildren;

        public string Name { get; set; }
        public NamedVariableScope UnresolvedParentScope { get; set; }

        public NamedVariableScope() : base() {
            Name = String.Empty;
            UnresolvedParentScope = null;
            UnresolvedChildren = new Dictionary<string, VariableScope>();
        }

        public string FullName {
            get {
                return GetFullName();
            }
        }

        public string UnresolvedName {
            get {
                return GetUnresolvedName();
            }
        }

        public override void AddChildScope(VariableScope childScope) {
            var cs = childScope as NamedVariableScope;
            if(cs != null) {
                AddNamedChildScope(cs);
            } else {
                base.AddChildScope(childScope);
            }
        }

        protected void AddNamedChildScope(NamedVariableScope childScope) {
            if(childScope.UnresolvedParentScope == null) {
                base.AddChildScope(childScope);
            } else {
                UnresolvedChildren[childScope.UnresolvedName] = childScope;
            }
        }

        public override bool IsSameAs(VariableScope otherScope) {
            return this.IsSameAs(otherScope as NamedVariableScope);
        }

        public virtual bool IsSameAs(NamedVariableScope otherScope) {
            return (null != otherScope && this.Name == otherScope.Name);
        }

        private string GetUnresolvedName() {
            StringBuilder sb = new StringBuilder();
            var current = UnresolvedParentScope;
            while(current != null) {
                sb.Append(current.Name);
                sb.Append(".");
                current = current.ChildScopes.FirstOrDefault() as NamedVariableScope;
            }

            return sb.ToString().TrimEnd('.');
        }

        private string GetFullName() {
            var scopes = from p in this.ParentScopes
                         let namedScope = p as NamedVariableScope
                         where namedScope != null && namedScope.Name.Length > 0
                         select namedScope.Name;
            StringBuilder sb = new StringBuilder();
            foreach(var scope in scopes.Reverse()) {
                sb.Append(scope);
                sb.Append(".");
            }
            sb.Append(this.Name);
            return sb.ToString();
        }
    }
}
