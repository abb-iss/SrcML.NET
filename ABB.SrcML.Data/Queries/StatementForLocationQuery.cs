/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *  Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Queries {
    /// <summary>
    /// This query object finds the deepest descendant of a statement that contains a given location
    /// </summary>
    public class StatementForLocationQuery : AbstractQuery<SourceLocation, Statement> {
        /// <summary>
        /// Create a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        public StatementForLocationQuery(AbstractWorkingSet workingSet, int lockTimeout) 
            : base(workingSet, lockTimeout) {}

        /// <summary>
        /// Create a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        /// <param name="factory">The task factory for asynchronous queries</param>
        public StatementForLocationQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// Gets the last descendant of <paramref name="root"/> that contains the given <paramref name="parameter">location</paramref>
        /// </summary>
        /// <param name="root">The statement to query</param>
        /// <param name="parameter">The location to find</param>
        /// <returns>The furthest descendant of <paramref name="root"/> that contains <paramref name="parameter"/></returns>
        public override Statement Execute(Statement root, SourceLocation parameter) {
            return Query(root, parameter);
        }

        /// <summary>
        /// Gets the last descendant of <paramref name="root"/> that contains the given <paramref name="parameter">location</paramref>
        /// </summary>
        /// <param name="root">The statement to query</param>
        /// <param name="parameter">The location to find</param>
        /// <returns>The furthest descendant of <paramref name="root"/> that contains <paramref name="parameter"/></returns>
        public static Statement Query(Statement root, SourceLocation parameter) {
            return root.GetStatementForLocation(parameter);
        }
    }

    /// <summary>
    /// This query object finds the deepest descendant of a statement that contains a given location and has type <typeparamref name="TStatement"/>
    /// </summary>
    /// <typeparam name="TStatement"></typeparam>
    public class StatementForLocationQuery<TStatement>
        : AbstractQuery<SourceLocation, TStatement> where TStatement : Statement, new() {
        /// <summary>
        /// Create a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        public StatementForLocationQuery(AbstractWorkingSet workingSet, int lockTimeout)
            : base(workingSet, lockTimeout) { }

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        /// <param name="factory">The task factory for asynchronous queries</param>
        public StatementForLocationQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory){ }

        /// <summary>
        /// Gets the last descendant of <paramref name="root"/> that contains the given <paramref name="parameter">location</paramref> of type <typeparamref name="TStatement"/>.
        /// Calls <see cref="Query"/>.
        /// </summary>
        /// <param name="root">The statement to query</param>
        /// <param name="parameter">The location to find</param>
        /// <returns>The furthest descendant of <paramref name="root"/> that contains <paramref name="parameter"/> of type <typeparamref name="TStatement"/></returns>
        public override TStatement Execute(Statement root, SourceLocation parameter) {
            return Query(root, parameter);
        }

        /// <summary>
        /// Gets the last descendant of <paramref name="root"/> that contains the given <paramref name="parameter">location</paramref> of type <typeparamref name="TStatement"/>
        /// </summary>
        /// <param name="root">The statement to query</param>
        /// <param name="parameter">The location to find</param>
        /// <returns>The furthest descendant of <paramref name="root"/> that contains <paramref name="parameter"/> of type <typeparamref name="TStatement"/></returns>
        public static TStatement Query(Statement root, SourceLocation parameter) {
            if(null != root) {
                var scope = StatementForLocationQuery.Query(root, parameter);
                return (null != scope ? scope.GetAncestorsAndSelf<TStatement>().FirstOrDefault() : default(TStatement));
            }
            return default(TStatement);
        }
    }
}
