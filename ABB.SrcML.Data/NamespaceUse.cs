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

        /// <summary>
        /// Determines the possible types for this expression. 
        /// Since this is a NamespaceUse, there are never any matching types.
        /// </summary>
        public override IEnumerable<TypeDefinition> ResolveType() {
            return Enumerable.Empty<TypeDefinition>();
        }

        /// <summary>
        /// Finds Namespaces that match this usage.
        /// </summary>
        public override IEnumerable<INamedEntity> FindMatches() {
            if(this.ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }

            //TODO: determine if we need to consider aliases in this method

            //If there's a calling expression, match and search under results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                return callingScopes.SelectMany(s => s.GetNamedChildren<NamespaceDefinition>(this.Name));
            }

            //search for namespace starting from the global root
            var globalNS = this.ParentStatement.GetAncestorsAndSelf<NamespaceDefinition>().FirstOrDefault(nd => nd.IsGlobal);
            if(globalNS == null) {
                throw new StatementDetachedException(this.ParentStatement);
            }
            return globalNS.GetNamedChildren<NamespaceDefinition>(this.Name);
        }
    }
}