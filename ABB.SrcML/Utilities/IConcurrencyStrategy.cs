/******************************************************************************
 * Copyright (c) 2014 ABB Group
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
    /// A concurrency strategy is used to compute the number of available
    /// cores for the <see cref="LimitedConcurrencyLevelTaskScheduler"/>
    /// </summary>
    public interface IConcurrencyStrategy {
        /// <summary>
        /// Returns the number of available cores
        /// </summary>
        /// <returns>The number of available cores</returns>
        int ComputeAvailableCores();
    }
}
