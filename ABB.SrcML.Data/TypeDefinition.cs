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
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class TypeDefinition : NamedVariableScope {
        public TypeDefinition() : base() {
            this.InnerTypes = new Collection<TypeDefinition>();
            this.Methods = new Collection<MethodDefinition>();
            this.Parents = new Collection<TypeUse>();
            this.IsPartial = false;
        }

        public AccessModifier Accessibility { get; set; }
        public Collection<VariableDeclaration> Fields { get; set; }
        public Collection<string> Filenames { get; set; }
        public Collection<TypeDefinition> InnerTypes { get; set; }
        public bool IsPartial { get; set; }
        public TypeKind Kind { get; set; }
        public Language Language { get; set; } //TODO: figure out where this should be specified
        public Collection<MethodDefinition> Methods { get; set; }
        
        public Collection<TypeUse> Parents { get; set; }

        public virtual bool IsSameAs(TypeDefinition otherScope) {
            return base.IsSameAs(otherScope) && this.IsPartial && otherScope.IsPartial;
        }

        public override bool IsSameAs(NamedVariableScope otherScope) {
            return this.IsSameAs(otherScope as TypeDefinition);
        }
    }
}
