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
        public string Name { get; set; }
        public NamedVariableScope UnresolvedParentScope { get; set; }

        public NamedVariableScope() : base() {
            Name = String.Empty;
            UnresolvedParentScope = null;
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
                var root = childScope.UnresolvedParentScope;
                
                VariableScope latest = root, current;

                do {
                    current = latest;
                    latest = current.ChildScopes.FirstOrDefault();
                } while(latest != null);
                
                childScope.UnresolvedParentScope = null;
                current.AddChildScope(childScope);
                base.AddChildScope(root);
            }
        }

        public override VariableScope Merge(VariableScope otherScope) {
            if(this.CanBeMergedWith(otherScope) && otherScope.CanBeMergedWith(this)) {
                // this and otherScope have the same name and the same type.
                // they can be merged normally
                return this.AddFrom(otherScope);
            } else if(!this.CanBeMergedWith(otherScope) && otherScope.CanBeMergedWith(this)) {
                // this is a subclass of NamedVariableScope and otherScope is a NamedVariableScope
                // useful information (type, method, or namespace data) are in this
                return this.AddFrom(otherScope);
            } else if(this.CanBeMergedWith(otherScope) && !otherScope.CanBeMergedWith(this)) {
                // this is a NamedVariableScope and otherScope is a subclass
                // useful information (type, method, or namespace data) are in otherscope
                return otherScope.AddFrom(this);
            }
            return null;
        }

        public override bool CanBeMergedWith(VariableScope otherScope) {
            return this.CanBeMergedWith(otherScope as NamedVariableScope);
        }

        public virtual bool CanBeMergedWith(NamedVariableScope otherScope) {
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
