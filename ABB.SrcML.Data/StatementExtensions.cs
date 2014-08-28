/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The Statement Extensions class contains helper extension methods for statements
    /// </summary>
    public static class StatementExtensions {
        /// <summary>
        /// Tests whether this method contains any calls to <paramref name="otherMethod"/>
        /// </summary>
        /// <param name="root">The statement to start searching from</param>
        /// <param name="otherMethod">The other method</param>
        /// <returns>True if any of the calls in this method are a match for <paramref name="otherMethod"/></returns>
        public static bool ContainsCallTo(this Statement root, MethodDefinition otherMethod) {
            if(null == otherMethod) { throw new ArgumentNullException("otherMethod"); }

            return root.GetCallsTo(otherMethod, true).Any();
        }

        /// <summary>
        /// Gets all of the method calls in this statement that matches <paramref name="otherMethod"/>
        /// </summary>
        /// <param name="root">The statement to start searching from</param>
        /// <param name="otherMethod">The other method</param>
        /// <param name="searchDescendantStatements">If true, this will return all the method calls to<paramref name="otherMethod"/> from <paramref name="root"/> and its descendants</param>
        public static IEnumerable<MethodCall> GetCallsTo(this Statement root, MethodDefinition otherMethod, bool searchDescendantStatements) {
            if(null == otherMethod) { throw new ArgumentNullException("otherMethod"); }

            //first filter calls for ones with the same name, number of parameters, etc.
            var initialMatches = root.FindExpressions<MethodCall>(searchDescendantStatements).Where(c => c.SignatureMatches(otherMethod)).ToList();
            if(initialMatches.Any()) {
                //check whether the call actually resolves to the other method
                foreach(var call in initialMatches) {
                    if(call.FindMatches().Any(m => m == otherMethod)) {
                        yield return call;
                    }
                }
            }
        }
    }
}
