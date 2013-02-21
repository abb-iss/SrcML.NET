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
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a use of a type. It is used in declarations and inheritance specifications.
    /// </summary>
    public class TypeUse : AbstractUse<TypeDefinition> {
        /// <summary>
        /// Create a new type use object.
        /// </summary>
        public TypeUse() {
            this.Name = String.Empty;
        }

        /// <summary>
        /// The prefix for this type use object
        /// </summary>
        public NamedScopeUse Prefix { get; set; }

        /// <summary>
        /// Tests if this type use is a match for the given <paramref name="definition"/>
        /// </summary>
        /// <param name="definition">the definition to compare to</param>
        /// <returns>true if the definitions match; false otherwise</returns>
        public override bool Matches(TypeDefinition definition) {
            return definition != null && definition.Name == this.Name;
        }
    }
}
