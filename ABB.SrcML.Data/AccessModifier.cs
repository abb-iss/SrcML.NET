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
    /// Enumerates the types of protection encountered in the supported programming languages
    /// </summary>
    public enum AccessModifier {
        /// <summary>None indicates that no access modifier was provided</summary>
        None = 0,
        /// <summary>Public</summary>
        Public,
        /// <summary>Protected</summary>
        Protected,
        /// <summary>Internal</summary>
        Internal,
        /// <summary>Private</summary>
        Private
    }
}
