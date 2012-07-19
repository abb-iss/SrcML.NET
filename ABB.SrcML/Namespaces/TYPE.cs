/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// SrcML Namespace for C/C++ modifiers
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "TYPE")]
    public sealed class TYPE
    {
        private TYPE()
        {

        }

        /// <summary>
        /// XNamespace for the SrcML Modifier XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcML/modifier";

        /// <summary>
        /// XMLNS prefix for the SrcML TYPE namespace
        /// </summary>
        public const string Prefix = "type";

        /// <summary>
        /// Argument string to pass to one of the srcml executables to enable this namespace
        /// </summary>
        public const string ArgumentLabel = "--modifier";

        /// <summary>
        /// Operator element -- all operators are surrounded with this:
        /// e.g. &lt;operator&gt;-&lt;&lt;/operator&gt;
        /// </summary>
        public static readonly XName Modifier = NS + "modifier";
    }
}
