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
    /// This interface provides methods that <see cref="AbstractUse"/> objects should implement if they should resolve to a return type
    /// <see cref="FindFirstMatchingType()"/> should be a call to <c><see cref="FindMatchingTypes()"/>.FirstOrDefault()</c>
    /// </summary>
    public interface IResolvesToType {
        /// <summary>
        /// Finds all of the possible matching types for this usage
        /// </summary>
        /// <returns>An enumerable of type definition objects</returns>
        IEnumerable<TypeDefinition> FindMatchingTypes();

        /// <summary>
        /// Returns the first matching type definition returned by <see cref="FindMatchingTypes()"/>
        /// </summary>
        /// <returns>The first matching type definition. Null if there aren't any.</returns>
        TypeDefinition FindFirstMatchingType();
    }
}
