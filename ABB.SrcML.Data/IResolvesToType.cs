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

using System.Collections.Generic;

namespace ABB.SrcML.Data {

    /// <summary>
    /// This interface provides methods that <see cref="IUse{T}"/> objects should implement if they
    /// should resolve to a return type <see cref="FindFirstMatchingType()"/> should be a call to
    /// <c><see cref="FindMatchingTypes()"/>.FirstOrDefault()</c>
    /// </summary>
    public interface IResolvesToType {

        /// <summary>
        /// The calling object
        /// </summary>
        IResolvesToType CallingObject { get; set; }

        /// <summary>
        /// The parent scope for this calling object
        /// </summary>
        IScope ParentScope { get; set; }

        /// <summary>
        /// Returns the first matching type definition returned by <see cref="FindMatchingTypes()"/>
        /// </summary>
        /// <returns>The first matching type definition. Null if there aren't any.</returns>
        ITypeDefinition FindFirstMatchingType();

        /// <summary>
        /// Finds all of the possible matching types for this usage
        /// </summary>
        /// <returns>An enumerable of type definition objects</returns>
        IEnumerable<ITypeDefinition> FindMatchingTypes();
    }
}