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
        /// The intialization expression for the using block.
        /// </summary>
        public Expression Initializer { get; set; }

    }
}
