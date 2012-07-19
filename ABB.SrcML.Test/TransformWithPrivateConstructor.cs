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

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ABB.SrcML;

namespace ABB.SrcML.Test
{
    public class TransformWithPrivateConstructor : ITransform
    {
        private TransformWithPrivateConstructor()
        {

        }
        public IEnumerable<XElement> Query(XElement element)
        {
            return Enumerable.Empty<XElement>();
        }

        public XElement Transform(XElement element)
        {
            return element;
        }
    }
}
