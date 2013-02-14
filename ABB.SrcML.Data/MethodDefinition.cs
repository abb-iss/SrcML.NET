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

        /// <summary>
        /// Creates a new method definition object
        /// </summary>
        public MethodDefinition() : base() {
            this._parameters = new Collection<VariableDeclaration>();
        }

        public MethodDefinition(MethodDefinition otherDefinition)
            : base(otherDefinition) {
            this._parameters = new Collection<VariableDeclaration>();
            foreach(var parameter in otherDefinition._parameters) {
                this._parameters.Add(parameter);
            }
        }
        /// <summary>
        /// The access modifier for this type
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary>
        /// True if this is a constructor; false otherwise
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a destructor; false otherwise
        /// </summary>
        public bool IsDestructor { get; set; }

        public Collection<TypeDefinition> InnerTypes;

        /// <summary>
        /// The parameters for this method. Replacing this collection causes the <see cref="VariableScope.DeclaredVariables"/> to be updated.
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

        /// <summary>
        /// Merges this method definition with <paramref name="otherScope"/>. This happens when <c>otherScope.CanBeMergedInto(this)</c> evaluates to true.
        /// </summary>
        /// <param name="otherScope">the scope to merge with</param>
        /// <returns>a new method definition from this and otherScope</returns>
        public override NamedVariableScope Merge(NamedVariableScope otherScope) {
            MethodDefinition mergedScope = null;
            if(otherScope.CanBeMergedInto(this)) {
                mergedScope = new MethodDefinition(this);
                mergedScope.AddFrom(otherScope);
            }

            return mergedScope;
        }

        /// <summary>
        /// Returns true if both this and <paramref name="otherScope"/> have the same name.
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if they are the same method; false otherwise.</returns>
        /// TODO implement better method merging
        public virtual bool CanBeMergedInto(MethodDefinition otherScope) {
            return base.CanBeMergedInto(otherScope);
        }

        /// <summary>
        /// Casts <paramref name="otherScope"/> to a <see cref="MethodDefinition"/> and calls <see cref="CanBeMergedInto(MethodDefinition)"/>
        /// </summary>
        /// <param name="otherScope">The scope to test</param>
        /// <returns>true if <see cref="CanBeMergedInto(MethodDefinition)"/> evaluates to true.</returns>
        public override bool CanBeMergedInto(NamedVariableScope otherScope) {
            return this.CanBeMergedInto(otherScope as MethodDefinition);
        }
    }
}
