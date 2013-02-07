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
    /// Enumerates the kinds of types encountered in the supported programming languages.
    /// </summary>
    public enum TypeKind {
        /// <summary>Class type</summary>
        Class = 0,
        /// <summary>Struct type</summary>
        Struct,
        /// <summary>Union type</summary>
        Union,
        /// <summary>Interface type</summary>
        Interface,
        /// <summary>Enumeration type</summary>
        Enumeration
    }
}
