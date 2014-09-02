/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine(ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.VisualStudio {
    /// <summary>
    /// Event arguments for an added working set factory
    /// </summary>
    public class WorkingSetFactoryAddedEventArgs : EventArgs {
        /// <summary>
        /// Create a new working set factory added event argument object
        /// </summary>
        public WorkingSetFactoryAddedEventArgs() : base() { }

        /// <summary>
        /// Create a new working set factory added event argument object
        /// </summary>
        /// <param name="addedWorkingSetFactory">The added working set factory</param>
        public WorkingSetFactoryAddedEventArgs(AbstractWorkingSetFactory addedWorkingSetFactory)
            : base() {
            WorkingSetFactory = addedWorkingSetFactory;
        }

        /// <summary>
        /// The working set factory that was added
        /// </summary>
        public AbstractWorkingSetFactory WorkingSetFactory { get; set; }
    }
}
