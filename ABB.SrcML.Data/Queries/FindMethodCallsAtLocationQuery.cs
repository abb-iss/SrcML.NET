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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Queries {
    /// <summary>
    /// This query finds all of the method calls at the given location
    /// </summary>
    public class FindMethodCallsAtLocationQuery : AbstractQuery<SourceLocation, Collection<MethodCall>> {
        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        public FindMethodCallsAtLocationQuery(AbstractWorkingSet workingSet, int lockTimeout) 
            : base(workingSet, lockTimeout) {}

        /// <summary>
        /// Creates a new query object
        /// </summary>
        /// <param name="workingSet">The working set to query</param>
        /// <param name="lockTimeout">The time in milliseconds to wait for the read lock</param>
        /// <param name="factory">The task factory to use for asynchronous methods</param>
        public FindMethodCallsAtLocationQuery(AbstractWorkingSet workingSet, int lockTimeout, TaskFactory factory)
            : base(workingSet, lockTimeout, factory) { }

        /// <summary>
        /// Finds the <see cref="StatementForLocationQuery">furthest descendant</see> of <paramref name="root"/> that contains <paramref name="parameter"/>
        /// and then identifies all of the methods descended from that statement.
        /// Calls <see cref="Query"/>.
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">The parameter to search for</param>
        /// <returns>A collection of method calls found at this location</returns>
        public override Collection<MethodCall> Execute(Statement root, SourceLocation parameter) {
            return Query(root, parameter);
        }

        /// <summary>
        /// Finds the <see cref="StatementForLocationQuery">furthest descendant</see> of <paramref name="root"/> that contains <paramref name="parameter"/>
        /// and then identifies all of the methods descended from that statement.
        /// </summary>
        /// <param name="root">The root to query</param>
        /// <param name="parameter">The parameter to search for</param>
        /// <returns>A collection of method calls found at this location</returns>
        public static Collection<MethodCall> Query(Statement root, SourceLocation parameter) {
            if(null != root) {
                var statement = StatementForLocationQuery.Query(root, parameter);
                if(null != statement) {
                    var calls = statement.FindExpressions<MethodCall>().Where(mc => mc.Location.Contains(parameter));
                    return new Collection<MethodCall>(calls.OrderByDescending(mc => mc.Location, new SourceLocationComparer()).ToList());
                }
            }
            return new Collection<MethodCall>();
        }

        private class SourceLocationComparer : Comparer<SourceLocation> {

            public override int Compare(SourceLocation x, SourceLocation y) {
                if(object.Equals(x, y))
                    return 0;
                if(x == null && y != null)
                    return -1;
                if(x != null && y == null)
                    return 1;

                var result = x.StartingLineNumber.CompareTo(y.StartingLineNumber);
                if(result == 0) {
                    result = x.StartingColumnNumber.CompareTo(y.StartingColumnNumber);
                }
                return result;
            }
        }
    }
}
