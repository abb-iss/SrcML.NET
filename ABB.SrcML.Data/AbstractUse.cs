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
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The base classes for use objects. Use objects represent a use of a <see cref="NamedScope"/>.
    /// </summary>
    public abstract class AbstractUse {
        /// <summary>
        /// The location of this use in the original source file and in srcML
        /// </summary>
        public SourceLocation Location { get; set; }

        /// <summary>
        /// The name being used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The scope that contains this use
        /// </summary>
        public Scope ParentScope { get; set; }

        /// <summary>
        /// The programming language for this scope
        /// </summary>
        public Language ProgrammingLanguage { get; set; }
    }
}
