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
    /// SrcML Namespace for literals
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "LIT")]
    public sealed class LIT
    {
        private LIT()
        {

        }

        /// <summary>
        /// XNamespace for the SrcML Literal XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcML/literal";

        /// <summary>
        /// XMLNS prefix for the SrcML LIT namespace
        /// </summary>
        public const string Prefix = "lit";

        /// <summary>
        /// Argument string to pass to one of the srcml executables to enable this namespace
        /// </summary>
        public const string ArgumentLabel = "--literal";

        /// <summary>
        /// Literal element -- all literals are surrounded with this:
        /// e.g. &lt;literal&gt;-&lt;&lt;/literal&gt;
        /// &lt;lit:literal type="string"&gt;"foo"&lt;/lit:literal&gt;
        /// </summary>
        public static readonly XName Literal = NS + "literal";
    }
}
