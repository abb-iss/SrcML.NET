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
        }

        public Collection<TypeDefinition> Types { get; set; }
        public Collection<MethodDefinition> Methods { get; set; }
        public Collection<VariableDeclaration> Variables { get; set; }

        public bool IsGlobal { get { return this.Name.Length == 0; } }

        public string MakeQualifiedName(string name) {
            if(this.Name.Length == 0)
                return name;
            return String.Format("{0}.{1}", this.Name, name);
        }
    }
}
