/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class NamespaceDefinition : NamedVariableScope {
        public NamespaceDefinition() : base() {
            this.Types = new Collection<TypeDefinition>();
            this.Methods = new Collection<MethodDefinition>();
            this.Variables = new Collection<VariableDeclaration>();
            this.IsAnonymous = false;
        }

        public Collection<TypeDefinition> Types { get; set; }
        public Collection<MethodDefinition> Methods { get; set; }
        public Collection<VariableDeclaration> Variables { get; set; }

        /// <summary>
        /// Returns true if this is an anonymous namespace
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// <para>Returns true if this namespace represents the global namespace</para>
        /// <para>A namespace is global if the <see cref="NamedVariableScope.Name"/> is <c>String.Empty</c></para>
        /// </summary>
        public bool IsGlobal { get { return this.Name.Length == 0 && !this.IsAnonymous && this.ParentScope == null; } }

        /// <summary>
        /// Returns the fully qualified name for the given type
        /// </summary>
        /// <param name="name">A name</param>
        /// <returns>the fully qualified name (made from this namespace definition and the given name)</returns>
        public string MakeQualifiedName(string name) {
            if(this.Name.Length == 0)
                return name;
            return String.Format("{0}.{1}", this.Name, name);
        }

        public virtual bool IsSameAs(NamespaceDefinition otherScope) {
            return base.IsSameAs(otherScope);
        }

        public override bool IsSameAs(NamedVariableScope otherScope) {
            return this.IsSameAs(otherScope as NamespaceDefinition);
        }
    }
}
