/******************************************************************************
 * Copyright (c) 2010 ABB Group
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

namespace ABB.SrcML
{
    /// <summary>
    /// The Query attribute is used to identify SrcML Query functions that can be tested by the SrcML Preview Addin.
    /// <seealso cref="QueryHarness"/>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class QueryAttribute : Attribute
    {
    }
}
