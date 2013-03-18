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
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The base classes for use objects. Use objects represent a use of a <see cref="NamedScope"/>.
    /// </summary>
    public abstract class AbstractUse<DEFINITION> where DEFINITION : class {
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
        public Scope ParentScope { get; set; }

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
        /// Finds matching <typeparamref name="DEFINITION"/> from the <see cref="ParentScopes"/> of this usage.
        /// </summary>
        /// <returns>An enumerable of <typeparamref name="DEFINITION"/> objects that <see cref="Matches">matches</see> this usage.</returns>
        public virtual IEnumerable<DEFINITION> FindMatches() {
            DEFINITION definition = null;
            foreach(var parent in this.ParentScopes) {
                definition = parent as DEFINITION;

                if(Matches(definition)) {
                    yield return definition;
                }

                var matchingChildren = from child in parent.GetChildScopesWithId(this.Name)
                                       let castedChild = child as DEFINITION
                                       where Matches(castedChild)
                                       select castedChild;

                foreach(var match in matchingChildren) {
                    yield return match;
                }
            }
        }

        /// <summary>
        /// Tests if this usage matches the provided <paramref name="definition"/>
        /// </summary>
        /// <param name="definition">The definition to compare to</param>
        /// <returns>true if they are a match; false otherwise</returns>
        public abstract bool Matches(DEFINITION definition);
    }
}
