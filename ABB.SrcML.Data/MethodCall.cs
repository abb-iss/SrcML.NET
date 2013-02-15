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
    public class MethodCall {
        /// <summary>
        /// The name of the method being called
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// True if this is a call to a constructor
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a call to a destructor
        /// </summary>
        public bool IsDestructor { get; set; }

        /// <summary>
        /// The scope that contains this method call
        /// </summary>
        public Scope ParentScope { get; set; }

        /// <summary>
        /// The location of this call in both the source file and XML
        /// </summary>
        public SourceLocation Location { get; set; }

        /// <summary>
        /// The arguments to this call
        /// </summary>
        public Collection<VariableUse> Arguments { get; set; }

        /// <summary>
        /// The calling object for this method
        /// </summary>
        public VariableUse Caller { get; set; }

        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            Arguments = new Collection<VariableUse>();
        }
    }
}
