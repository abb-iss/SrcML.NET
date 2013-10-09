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
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a variable declaration
    /// </summary>
    [Serializable]
    public class VariableDeclaration {
        private Scope parentScope;

        /// <summary>
        /// The access modifier assigned to this type
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary>
        /// The location of this declaration in both the original source file and in XML.
        /// </summary>
        public virtual SrcMLLocation Location { get; set; }

        /// <summary>
        /// The name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope where this variable is declared
        /// </summary>
        public Scope Scope {
            get { return this.parentScope; }
            set {
                this.parentScope = value;
                if(null != VariableType) {
                    this.VariableType.ParentScope = this.parentScope;
                }
            }
        }

        /// <summary>
        /// Description of the type for this variable
        /// </summary>
        public virtual TypeUse VariableType { get; set; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            if(Accessibility != AccessModifier.None) {
                return string.Format("{0} {1} {2}", Accessibility.ToKeywordString(), VariableType, Name);
            } else {
                return string.Format("{0} {1}", VariableType, Name);
            }
        }
    }
}