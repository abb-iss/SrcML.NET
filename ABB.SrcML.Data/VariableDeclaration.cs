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
    public class VariableDeclaration {
        /// <summary>
        /// The name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the type for this variable
        /// </summary>
        public TypeUse VariableType { get; set; }

        /// <summary>
        /// The access modifier assigned to this type
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary>
        /// The scope where this variable is declared
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// XPath identifying the XML this declaration was created from
        /// </summary>
        public string XPath { get; set; }
    }
}
