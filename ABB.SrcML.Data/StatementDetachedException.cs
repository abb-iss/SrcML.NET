/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Indicates that a given Statement does not have a global namespace (i.e. NamespaceDefinition.IsGlobal == true)
    /// as an ancestor.
    /// </summary>
    public class StatementDetachedException : Exception {
        /// <summary>
        /// The Statement that is detached.
        /// </summary>
        public Statement DetachedStatement;

        /// <summary>
        /// Creates a new exception.
        /// </summary>
        /// <param name="detachedStatement">The Statement that is detached.</param>
        public StatementDetachedException(Statement detachedStatement)
            : this(detachedStatement, String.Format("{0} is not attached to a global namespace", detachedStatement)) {
        }

        /// <summary>
        /// Creates a new exception.
        /// </summary>
        /// <param name="detachedStatement">The Statement that is detached.</param>
        /// <param name="message">The desired message for the exception.</param>
        public StatementDetachedException(Statement detachedStatement, string message)
            : base(message) {
            this.DetachedStatement = detachedStatement;
        }
    }
}