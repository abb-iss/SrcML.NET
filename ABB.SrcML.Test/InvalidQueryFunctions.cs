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

namespace ABB.SrcML.Test
{
    class InvalidQueryFunctions
    {
        [Query]
        private IEnumerable<XElement> test1(XElement element)
        {
            var results = from r in element.Descendants(SRC.Block)
                          select r;
            return results;
        }

        [Query]
        public IEnumerable<XElement> test1(XElement element, XElement element2)
        {
            var results = from e in element.Descendants(SRC.Function)
                          select e;
            return results;
        }

        public IEnumerable<XElement> test2(XName name)
        {
            return Enumerable.Empty<XElement>();
        }
    }
}
