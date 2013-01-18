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
        public NamespaceDefinition CurrentNamespace { get; set; }

        /// <summary>
        /// Returns the possible names for this type use
        /// </summary>
        /// <returns>The possible full qualified names for this type use</returns>
        public IEnumerable<string> GetPossibleNames() {
            yield return CurrentNamespace.MakeQualifiedName(this.Name);

            if(!CurrentNamespace.IsGlobal)
                yield return this.Name;
        }
    }
}
