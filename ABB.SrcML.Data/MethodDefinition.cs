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
    /// <summary>
    /// A method definition object.
    /// </summary>
    public class MethodDefinition : NamedVariableScope {
        private Collection<VariableDeclaration> _parameters;

        public MethodDefinition() : base() {
            this._parameters = new Collection<VariableDeclaration>();
        }
        public AccessModifier Accessibility { get; set; }
        public bool IsConstructor { get; set; }
        public bool IsDestructor { get; set; }

        public Collection<TypeDefinition> InnerTypes;
        /// <summary>
        /// The parameters for this method. Replacing this collection causes the <see cref="VariableScope.DelcaredVariables"/> to be updated.
        /// </summary>
        /// TODO make the updating of the parameters collection more robust (you can't add an element to it and have DeclaredVariables updated.
        public Collection<VariableDeclaration> Parameters {
            get { return this._parameters; }
            set {
                var oldParameters = this._parameters;
                this._parameters = value;
                
                foreach(var parameter in oldParameters) {
                    this.DeclaredVariablesDictionary.Remove(parameter.Name);
                }
                
                foreach(var parameter in this._parameters) {
                    this.AddDeclaredVariable(parameter);
                }
            }
        }

        public virtual bool IsSameAs(MethodDefinition otherScope) {
            return base.IsSameAs(otherScope);
        }

        public override bool IsSameAs(NamedVariableScope otherScope) {
            return this.IsSameAs(otherScope as MethodDefinition);
        }
    }
}
