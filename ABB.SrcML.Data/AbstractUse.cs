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

namespace ABB.SrcML.Data {
    /// <summary>
    /// The base classes for use objects. Use objects represent a use of a <see cref="NamedScope"/>.
    /// </summary>
    public abstract class AbstractUse<DEFINITION> where DEFINITION : class {
        private List<Alias> internalAliasCollection;
        private Scope parentScope;

        /// <summary>
        /// Sets up the an abstract use object
        /// </summary>
        protected AbstractUse() {
            internalAliasCollection = new List<Alias>();
            Aliases = new ReadOnlyCollection<Alias>(internalAliasCollection);
        }
        /// <summary>
        /// The aliases for this type use
        /// </summary>
        public ReadOnlyCollection<Alias> Aliases { get; private set; }

        /// <summary>
        /// The location of this use in the original source file and in srcML
        /// </summary>
        public SrcMLLocation Location { get; set; }

        /// <summary>
        /// The name being used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope that contains this use
        /// </summary>
        public virtual Scope ParentScope {
            get { return this.parentScope; }
            set { this.parentScope = value; }
        }

        /// <summary>
        /// All of the parent scopes of this usage (from closest to farthest)
        /// </summary>
        public IEnumerable<Scope> ParentScopes {
            get {
                Scope current = ParentScope;
                while(null != current) {
                    yield return current;
                    current = current.ParentScope;
                }
            }
        }

        /// <summary>
        /// The programming language for this scope
        /// </summary>
        public Language ProgrammingLanguage { get; set; }

        /// <summary>
        /// Adds an alias. If <see cref="Alias.IsAliasFor{T}(AbstractUse{T})"/> returns false, then the alias is not added.
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
        /// Finds matching <typeparamref name="DEFINITION"/> from the <see cref="ParentScopes"/> of this usage.
        /// </summary>
        /// <returns>An enumerable of <typeparamref name="DEFINITION"/> objects that <see cref="Matches">matches</see> this usage.</returns>
        public abstract IEnumerable<DEFINITION> FindMatches();

        /// <summary>
        /// Tests if this usage matches the provided <paramref name="definition"/>
        /// </summary>
        /// <param name="definition">The definition to compare to</param>
        /// <returns>true if they are a match; false otherwise</returns>
        public abstract bool Matches(DEFINITION definition);

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            return Name;
        }
    }
}
