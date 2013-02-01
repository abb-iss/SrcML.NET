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
    /// <para>The VariableScopeIterator returns an enumerable of a <see cref="VariableScope"/> and all of its descendants.</para>
    /// <para>It works by yielding the element, and then calling Visit on each of the child scopes.</para>
    /// </summary>
    public class VariableScopeIterator {
        /// <summary>
        /// Dummy constructor
        /// </summary>
        public VariableScopeIterator() {
        }

        /// <summary>
        /// Visits all the nodes in the scope graph rooted at <paramref name="scope"/>
        /// </summary>
        /// <param name="scope">the root scope</param>
        /// <returns>An enumerable of all the scopes rooted at <paramref name="scope"/></returns>
        public IEnumerable<VariableScope> VisitScope(VariableScope scope) {
            yield return scope;
            foreach(var child in scope.ChildScopes) {
                foreach(var descendant in VisitScope(child)) {
                    yield return descendant;
                }
            }
        }

        /// <summary>
        /// Convenience method for constructing the iterator and visiting the variable scope.
        /// </summary>
        /// <param name="scope">the root scope</param>
        /// <returns>An enumerable of all the scopes rooted at <paramref name="scope"/></returns>
        public static IEnumerable<VariableScope> Visit(VariableScope scope) {
            return (new VariableScopeIterator()).VisitScope(scope);
        }
    }
}
