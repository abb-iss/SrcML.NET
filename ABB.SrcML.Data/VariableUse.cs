/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The variable use class represents a use of a variable.
    /// </summary>
    public class VariableUse {
        /// <summary>
        /// The name of the variable being used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope that contains this variable usage.
        /// </summary>
        public Scope ParentScope { get; set; }

        /// <summary>
        /// The location of this variable use in both the source code and the XML
        /// </summary>
        public SourceLocation Location { get; set; }
    }
}
