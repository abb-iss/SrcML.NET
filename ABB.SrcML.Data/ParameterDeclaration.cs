/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a parameter declaration in a method.
    /// </summary>
    public class ParameterDeclaration {
        /// <summary>The name of the parameter.</summary>
        public string Name { get; set; }

        /// <summary>Description of the type for this parameter.</summary>
        public TypeUse VariableType { get; set; }

        //TODO: account for modifiers that can exist on a parameter. E.g. ref, out, etc.

        /// <summary>The method where this parameter is declared.</summary>
        public MethodDefinition Method { get; set; }

        /// <summary>
        /// The locations where this parameter is declared.
        /// There can be more than one in the case of C/C++ where both the method prototype and definition declare the parameter.
        /// </summary>
        public Collection<SrcMLLocation> Locations { get; private set; }

        /// <summary>Creates a new empty ParameterDeclaration.</summary>
        public ParameterDeclaration() {
            Locations = new Collection<SrcMLLocation>();
        }

        /// <summary>Returns a string representation of this object.</summary>
        public override string ToString() {
            return string.Format("{0} {1}", VariableType, Name);
        }
    }
}
