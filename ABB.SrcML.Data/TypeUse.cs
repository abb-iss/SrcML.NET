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
        public AbstractCodeParser Parser { get; set; }
        public NamespaceDefinition CurrentNamespace { get; set; }
        public Collection<Alias> Aliases { get; set; }
        /// <summary>
        /// Returns the possible names for this type use
        /// </summary>
        /// <returns>The possible full qualified names for this type use</returns>
        public IEnumerable<string> GetPossibleNames() {
            return this.Parser.GeneratePossibleNamesForTypeUse(this);

            yield return CurrentNamespace.MakeQualifiedName(this.Name);

            var aliases = from alias in this.Aliases
                          where alias.IsAliasFor(this)
                          select alias.MakeQualifiedName(this);
            
            foreach(var alias in aliases) {
                yield return alias;
            }

            if(!CurrentNamespace.IsGlobal)
                yield return this.Name;
        }
    }
}
