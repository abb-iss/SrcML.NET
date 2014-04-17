/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    /// <summary>
    /// Event arguments for IsReady changed events
    /// </summary>
    public class IsReadyChangedEventArgs : EventArgs {
        /// <summary>
        /// The updated ready state
        /// </summary>
        public bool ReadyState { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        protected IsReadyChangedEventArgs() { }

        /// <summary>
        /// Constructs a new object
        /// </summary>
        /// <param name="updatedReadyState">The updated ready state</param>
        public IsReadyChangedEventArgs(bool updatedReadyState) {
            this.ReadyState = updatedReadyState;
        }
    }
}
