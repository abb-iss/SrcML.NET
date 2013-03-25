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

        /// <summary>
        /// The aliases for this type use
        /// </summary>
        public ReadOnlyCollection<Alias> Aliases { get; private set; }

        /// <summary>
        /// The calling object for this type (should be unused)
        /// </summary>
        public IResolvesToType CallingObject { get; set; }

        /// <summary>
        /// The prefix for this type use object
        /// </summary>
        public NamedScopeUse Prefix { get; set; }

        /// <summary>
        /// Adds an alias. If <see cref="Alias.IsAliasFor(TypeUse)"/> returns false, then the alias is not added.
        /// </summary>
        /// <param name="alias">The alias to add</param>
        public void AddAlias(Alias alias) {
            if(alias.IsAliasFor(this)) {
                internalAliasCollection.Add(alias);
            }
        }

        /// <summary>
        /// Adds an enumerable of aliases to this scope.
        /// </summary>
        /// <param name="aliasesToAdd">The aliases to add</param>
        public void AddAliases(IEnumerable<Alias> aliasesToAdd) {
            if(aliasesToAdd != null) {
                foreach(var alias in aliasesToAdd) {
                    this.AddAlias(alias);
                }
            }
        }

        /// <summary>
        /// Finds all of the matches for this type
        /// </summary>
        /// <returns>All of the type definitions that match this type use</returns>
        public override IEnumerable<TypeDefinition> FindMatches() {
            // if this is a built-in type, then just return that
            // otherwise, go hunting for matching types
            if(BuiltInTypeFactory.IsBuiltIn(this)) {
                yield return BuiltInTypeFactory.GetBuiltIn(this);
            } else {
                // First, jsut call AbstractUse.FindMatches() this will search everything in ParentScope.GetParentScopesAndSelf<TypeDefinition>()
                // for a matching type and return it
                foreach(var match in base.FindMatches()) {
                    yield return match;
                }

                // once we've done that, examine the aliases
                if(Aliases.Count > 0) {
                    // first, find a global scope -- we currently search for aliases starting at the global scope
                    var globalScope = (from ns in this.ParentScope.GetParentScopesAndSelf<NamespaceDefinition>()
                                       where ns.IsGlobal
                                       select ns).FirstOrDefault();
                    // there are issues if we don't find a global scope -- something has gone wrong, or this use is disconnected
                    if(globalScope != null) {
                        // TODO can the namespace code be moved to NamespaceUse?
                        foreach(var alias in this.Aliases) {
                            // TODO handle type/method imports
                            if(alias.IsNamespaceImport) {
                                var currentNsUse = alias.ImportedNamespace;

                                List<Scope> scopes = new List<Scope>();
                                scopes.Add(globalScope);
                                
                                // we will go through each namespace referenced by the alias
                                while(currentNsUse != null) {
                                    // go through all of the scopes and get the children that match currentNsUse
                                    // on the first iteration, the only thing in scopes will be the global scope
                                    // on subsequent iterations, scopes will contain matches for the parent of currentNsUse
                                    int currentLength = scopes.Count;
                                    for(int i = 0; i < currentLength; i++) {
                                        var matches = from scope in scopes[i].GetChildScopes<NamedScope>()
                                                      where scope.Name == currentNsUse.Name
                                                      select scope;
                                        scopes.AddRange(matches);
                                    }
                                    // once we've found matches for currentNsUse, remove the previous scopes from the list
                                    // and set currentNsUse to its child
                                    scopes.RemoveRange(0, currentLength);
                                    currentNsUse = currentNsUse.ChildScopeUse as NamespaceUse;
                                }

                                // The answers identify namespaces that match this alias
                                // now we look at each matching namespace and find the types that actually match this TypeUse.
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

        /// <summary>
        /// Gets the first type that matches this use
        /// </summary>
        /// <returns>The matching type; null if there aren't any</returns>
        public TypeDefinition FindFirstMatchingType() {
            return this.FindMatches().FirstOrDefault();
        }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            if(Prefix != null) {
                return string.Format("{0}.{1}", Prefix, Name);
            } else {
                return Name;
            }
        }
    }
}
