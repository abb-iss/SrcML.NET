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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class TypeUse {
        public string Name { get; set; }
        public string NamespaceName { get; set; }
        public Collection<string> ImportedNamespaces { get; set; }

        internal IEnumerable<string> GetPossibleNames() {
            throw new NotImplementedException();
        }
    }
}
