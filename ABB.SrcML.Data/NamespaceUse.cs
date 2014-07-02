/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents the use of a namespace. This is primarily used in <see cref="AliasStatement"/> and <see cref="ImportStatement"/> objects.
    /// </summary>
    public class NamespaceUse : NameUse {
        /// <summary> The XML name for NamespaceUse </summary>
        public new const string XmlName = "nsu";

        /// <summary>
        /// Instance method for getting <see cref="NamespaceUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for NamespaceUse</returns>
        public override string GetXmlName() { return NamespaceUse.XmlName; }

        public override IEnumerable<TypeDefinition> ResolveType() {
            return null;
        }

        public override IEnumerable<INamedEntity> FindMatches() {
            throw new NotImplementedException();
            
            var siblings = GetSiblingsBeforeSelf().ToList();
            var nameInclusionOperators = new[] {".", "->", "::"};
            var priorOp = siblings.Last() as OperatorUse;
            if(priorOp != null && nameInclusionOperators.Contains(priorOp.Text)) {
                
            }


        }
    }
}