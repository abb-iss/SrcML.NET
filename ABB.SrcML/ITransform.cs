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

namespace ABB.SrcML
{
    /// <summary>
    /// Interface for SrcML transformations.
    /// </summary>
    public interface ITransform
    {
        /// <summary>
        /// Find each matching element rooted at the given element.
        /// </summary>
        /// <param name="element">the rootUnit element</param>
        /// <returns>the matching elements</returns>
        IEnumerable<XElement> Query(XElement element);

        /// <summary>
        /// Transform the given element. Typically, the input for this function should come from <see cref="QueryAttribute"/>.
        /// <code lang="C#">
        /// foreach(var e in transform.QueryAttribute(element))
        ///     e.ReplaceWith(transform.Transform(e);
        /// </code>
        /// </summary>
        /// <param name="element">the element to transform. Typically comes from <see cref="QueryAttribute"/></param>
        /// <returns>a transformed version of the </returns>
        XElement Transform(XElement element);
    }
}
