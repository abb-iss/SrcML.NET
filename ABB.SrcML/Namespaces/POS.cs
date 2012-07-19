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
    /// SrcML Namespace for source position.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "POS")]
    public sealed class POS
    {
        private POS()
        {

        }

        /// <summary>
        /// XNamespace for the SrcML Position XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcML/position";

        /// <summary>
        /// XMLNS prefix for the SrcML OP namespace
        /// </summary>
        public const string Prefix = "pos";

        /// <summary>
        /// Argument string to pass to one of the srcml executables to enable this namespace
        /// </summary>
        public const string ArgumentLabel = "--position";

        /// <summary>
        /// This attribute indicates the line number that the source element is located on
        /// </summary>
        public static readonly XName Line = NS + "line";

        /// <summary>
        /// This attribute indicates the column that the source element starts at
        /// </summary>
        public static readonly XName Column = NS + "column";
        
    }
}
