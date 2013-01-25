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
    public class TypeDefinition {
        public string Name { get; set; }
        public AccessModifier Accessibility { get; set; }
        public Collection<VariableDeclaration> Fields { get; set; }
        public Collection<string> Filenames { get; set; }
        public Collection<TypeDefinition> InnerTypes { get; set; }
        public bool IsPartial { get; set; }
        public TypeKind Kind { get; set; }
        public Language Language { get; set; } //TODO: figure out where this should be specified
        public Collection<MethodDefinition> Methods { get; set; }
        public NamespaceDefinition Namespace { get; set; } //TODO: do we need this?
        public Collection<TypeUse> Parents { get; set; }
        public string XPath { get; set; }

        public XElement GetXElement() {
            return null;
        }

        public string GetFullName() {
            return this.Namespace.MakeQualifiedName(this.Name);
        }
    }
}
