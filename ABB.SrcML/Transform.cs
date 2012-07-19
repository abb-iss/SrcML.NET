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
using System.Xml.Linq;
using System.Reflection;

namespace ABB.SrcML
{
    /// <summary>
    /// A delegate for SrcML query functions.
    /// </summary>
    /// <param name="element">The XML node to query from.</param>
    /// <returns>A list of matching nodes from <paramref name="element"/>.</returns>
    public delegate IEnumerable<XElement> Query(XElement element);

    /// <summary>
    /// A delegate for SrcML transform functions.
    /// </summary>
    /// <param name="element">the element to transform</param>
    public delegate XElement Transform(XElement element);
}
