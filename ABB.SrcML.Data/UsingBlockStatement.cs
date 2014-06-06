/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Represents a using block statement in C#.
    /// These are of the form: 
    /// <code> using(Foo f = new Foo()) { ... } </code>
    /// Note that this is different from a using directive, e.g. <code>using System.Text;</code>
    /// </summary>
    public class UsingBlockStatement : BlockStatement
    {
        /// <summary>
        /// The variable declarations for the using block.
        /// If there is only one, this property will be a VariableDeclaration.
        /// If there are more than one, this property will be an expression with multiple VariableDeclarations as components.
        /// </summary>
        public Expression Declarations { get; set; }

    }
}
