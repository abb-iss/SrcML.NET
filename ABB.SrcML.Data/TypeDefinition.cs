﻿/******************************************************************************
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
    /// <summary>
    /// Represents a type definition
    /// </summary>
    public class TypeDefinition : NamedVariableScope {
        /// <summary>
        /// Creates a new type definition object
        /// </summary>
        public TypeDefinition() : base() {
            this.InnerTypes = new Collection<TypeDefinition>();
            this.Methods = new Collection<MethodDefinition>();
            this.Parents = new Collection<TypeUse>();
            this.IsPartial = false;
        }

        /// <summary>
        /// The access modifier for this type
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        public Collection<VariableDeclaration> Fields { get; set; }
        public Collection<string> Filenames { get; set; }
        public Collection<TypeDefinition> InnerTypes { get; set; }

        /// <summary>
        /// Partial if this is a partial class (used in C#)
        /// </summary>
        public bool IsPartial { get; set; }

        /// <summary>
        /// The <see cref="TypeKind"/> of this type
        /// </summary>
        public TypeKind Kind { get; set; }
        public Language Language { get; set; } //TODO: figure out where this should be specified
        public Collection<MethodDefinition> Methods { get; set; }
        
        /// <summary>
        /// The parent types that this type inherits from
        /// </summary>
        public Collection<TypeUse> Parents { get; set; }

        /// <summary>
        /// Returns true if both this and <paramref name="otherScope"/> have the same name and are both partial.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same class; false otherwise.</returns>
        public virtual bool CanBeMergedWith(TypeDefinition otherScope) {
            return base.CanBeMergedWith(otherScope) && this.IsPartial && otherScope.IsPartial;
        }

        /// <summary>
        /// Casts <paramref name="otherScope"/> to a <see cref="TypeDefinition"/> and calls <see cref="CanBeMergedWith(TypeDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedWith(TypeDefinition)"/> evaluates to true.</returns>
        public override bool CanBeMergedWith(NamedVariableScope otherScope) {
            return this.CanBeMergedWith(otherScope as TypeDefinition);
        }
    }
}
