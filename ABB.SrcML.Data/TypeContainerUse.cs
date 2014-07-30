/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class TypeContainerUse : NameUse {
        /// <summary> The XML name for TypeContainerUse </summary>
        public new const string XmlName = "TypeContainerUse";

        /// <summary>
        /// Instance method for getting <see cref="TypeContainerUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for TypeContainerUse</returns>
        public override string GetXmlName() {
            return TypeContainerUse.XmlName;
        }

        /// <summary>
        /// Finds TypeDefinitions or NamespaceDefinitions that match this name.
        /// </summary>
        public override IEnumerable<INamedEntity> FindMatches() {
            //return base.FindMatches();
            
            if(ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }

            //handle keywords
            if(Name == "this" ||
               (Name == "base" && ProgrammingLanguage == Language.CSharp) ||
               (Name == "super" && ProgrammingLanguage == Language.Java)) {
                return TypeDefinition.GetTypeForKeyword(this);
            }

            //If there's a prefix, resolve that and search under results
            if(Prefix != null) {
                return Prefix.FindMatches().SelectMany(ns => ns.GetNamedChildren<NamedScope>(this.Name)).Where(e => e is TypeDefinition || e is NamespaceDefinition);
            }

            //If there's a calling expression, match and search under results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                return callingScopes.SelectMany(s => s.GetNamedChildren<NamedScope>(this.Name)).Where(e => e is TypeDefinition || e is NamespaceDefinition);
            }

            ////Search for local variables
            //var localVars = SearchForLocalVariable().ToList();
            //if(localVars.Any()) {
            //    return localVars;
            //}

            //search the surrounding type and its base types
            var containingType = ParentStatement.GetAncestors<TypeDefinition>().FirstOrDefault();
            if(containingType != null) {
                var parentMatches = containingType.GetParentTypesAndSelf(true).SelectMany(t => t.GetNamedChildren<NamedScope>(this.Name)).Where(e => e is TypeDefinition || e is NamespaceDefinition).ToList();
                if(parentMatches.Any()) {
                    return parentMatches;
                }
            }

            //search the surrounding scopes
            var lex = ParentStatement.GetAncestorsAndSelf<NamedScope>().SelectMany(ns => ns.GetNamedChildren<NamedScope>(this.Name)).Where(e => e is TypeDefinition || e is NamespaceDefinition).ToList();
            if(lex.Any()) {
                return lex;
            }

            //search if there is an alias for this name
            foreach(var alias in GetAliases()) {
                if(alias.AliasName == this.Name) {
                    var targetName = alias.Target as NameUse;
                    if(targetName == null) {
                        //Target is not a NameUse, probably an Expression
                        targetName = alias.Target.GetDescendantsAndSelf<NameUse>().LastOrDefault();
                    }
                    if(targetName != null) {
                        return targetName.FindMatches();
                    }
                }
            }

            //we didn't find it locally, search under imported namespaces
            return (from import in GetImports()
                    from match in import.ImportedNamespace.GetDescendantsAndSelf<NameUse>().Last().FindMatches()
                    from child in match.GetNamedChildren<NamedScope>(this.Name)
                    where child is TypeDefinition || child is NamespaceDefinition
                    select child);
        }

        /// <summary>
        /// Determines the possible types of this expression.
        /// </summary>
        /// <returns>An enumerable of the matching TypeDefinitions for this expression's possible types.</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            return FindMatches().OfType<TypeDefinition>();
        }
    }
}
