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
    /// <summary>
    /// Represents a use of a type. It is used in declarations and inheritance specifications.
    /// </summary>
    public class TypeUse : AbstractUse<TypeDefinition>, IResolvesToType {
        private List<Alias> internalAliasCollection;

        /// <summary>
        /// Create a new type use object.
        /// </summary>
        public TypeUse() {
            this.Name = String.Empty;
            this.internalAliasCollection = new List<Alias>();
            this.Aliases = new ReadOnlyCollection<Alias>(internalAliasCollection);
        }

        public ReadOnlyCollection<Alias> Aliases { get; private set; }

        public IResolvesToType CallingObject { get; set; }

        /// <summary>
        /// The prefix for this type use object
        /// </summary>
        public NamedScopeUse Prefix { get; set; }

        public void AddAlias(Alias alias) {
            if(alias.IsAliasFor(this)) {
                internalAliasCollection.Add(alias);
            }
        }

        public void AddAliases(IEnumerable<Alias> aliasesToAdd) {
            if(aliasesToAdd != null) {
                foreach(var alias in aliasesToAdd) {
                    this.AddAlias(alias);
                }
            }
        }

        public override IEnumerable<TypeDefinition> FindMatches() {
            if(BuiltInTypeFactory.IsBuiltIn(this)) {
                yield return BuiltInTypeFactory.GetBuiltIn(this);
            } else {
                foreach(var match in base.FindMatches()) {
                    yield return match;
                }

                if(Aliases.Count > 0) {
                    var globalScope = (from ns in this.ParentScope.GetParentScopesAndSelf<NamespaceDefinition>()
                                       where ns.IsGlobal
                                       select ns).FirstOrDefault();
                    if(globalScope != null) {
                        foreach(var alias in this.Aliases) {
                            if(alias.IsNamespaceImport) {
                                var currentNsUse = alias.ImportedNamespace;

                                List<Scope> scopes = new List<Scope>();
                                scopes.Add(globalScope);
                                
                                while(currentNsUse != null) {
                                    int currentLength = scopes.Count;
                                    for(int i = 0; i < currentLength; i++) {
                                        var matches = from scope in scopes[i].GetChildScopes<NamedScope>()
                                                      where scope.Name == currentNsUse.Name
                                                      select scope;
                                        scopes.AddRange(matches);
                                    }
                                    scopes.RemoveRange(0, currentLength);
                                    currentNsUse = currentNsUse.ChildScopeUse as NamespaceUse;
                                }

                                var answers = from scope in scopes
                                              from typeDefinition in scope.GetChildScopes<TypeDefinition>()
                                              where typeDefinition.Name == this.Name
                                              select typeDefinition;

                                foreach(var answer in answers) {
                                    yield return answer;
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Tests if this type use is a match for the given <paramref name="definition"/>
        /// </summary>
        /// <param name="definition">the definition to compare to</param>
        /// <returns>true if the definitions match; false otherwise</returns>
        public override bool Matches(TypeDefinition definition) {
            return definition != null && definition.Name == this.Name;
        }

        /// <summary>
        /// This is just a call to <see cref="FindMatches()"/>
        /// </summary>
        /// <returns>The matching type definitions for this use</returns>
        public IEnumerable<TypeDefinition> FindMatchingTypes() {
            return this.FindMatches();
        }

        public TypeDefinition FindFirstMatchingType() {
            return this.FindMatches().FirstOrDefault();
        }
    }
}
