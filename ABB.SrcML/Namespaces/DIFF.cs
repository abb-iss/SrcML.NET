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
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// The DIFF class contains all of the XNames for SrcML Diff namespace.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "DIFF")]
    public static class DIFF
    {
        /// <summary>
        /// XNamespace for the SrcML SRC XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcDiff";

        /// <summary>
        /// XMLNS prefix for the SrcML SRC namespace
        /// </summary>
        public const string Prefix = "diff";

        /// <summary>
        /// Markup for the diff:insert tag
        /// </summary>
        public static readonly XName Insert = NS + "insert";

        /// <summary>
        /// Markup for the diff:delete tag
        /// </summary>
        public static readonly XName Delete = NS + "delete";

        /// <summary>
        /// Markup for the diff:common tag
        /// </summary>
        public static readonly XName Common = NS + "common";

        // type attribute constants
        /// <summary>
        /// XName for the "type" attribute. The value of this attribute can be either TypeWhitespace or TypeChange
        /// </summary>
        public static readonly XName TypeAttribute = "type";

        /// <summary>
        /// The "whitespace" value for <see cref="TypeAttribute"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace")]
        public const string TypeWhitespace = "whitespace";

        /// <summary>
        /// /// The "change" value for <see cref="TypeAttribute"/>
        /// </summary>
        public const string TypeChange = "change";
    }
}
