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

namespace ABB.SrcML.Data {

    /// <summary>
    /// Event arguments for capturing a thrown exception
    /// </summary>
    public class ErrorRaisedArgs : EventArgs {

        /// <summary>
        /// Constructs a new event args object
        /// </summary>
        /// <param name="exception"></param>
        public ErrorRaisedArgs(Exception exception) {
            this.Exception = exception;
        }

        /// <summary>
        /// The exception that was thrown
        /// </summary>
        public Exception Exception { get; private set; }
    }
}