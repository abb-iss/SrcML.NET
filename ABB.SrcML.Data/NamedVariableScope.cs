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

        public string GetFullName() {
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

        public override bool IsSameAs(VariableScope otherScope) {
            return this.IsSameAs(otherScope as NamedVariableScope);
        }

        public virtual bool IsSameAs(NamedVariableScope otherScope) {
            return (null != otherScope && this.Name == otherScope.Name);
        }
    }
}
