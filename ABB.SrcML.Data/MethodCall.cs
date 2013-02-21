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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a method call
    /// </summary>
    public class MethodCall : AbstractUse {
        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            Arguments = new Collection<VariableUse>();
            IsConstructor = false;
            IsDestructor = false;
        }

        /// <summary>
        /// The arguments to this call
        /// </summary>
        public Collection<VariableUse> Arguments { get; set; }

        /// <summary>
        /// The calling object for this method
        /// </summary>
        public VariableUse Caller { get; set; }

        /// <summary>
        /// True if this is a call to a constructor
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a call to a destructor
        /// </summary>
        public bool IsDestructor { get; set; }
    }
}
