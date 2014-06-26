/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - Initial implementation
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace ABB.SrcML {

    /// <summary>
    /// Maintains a mapping between source file paths and the paths where XML versions are stored.
    /// The names of the XML files are relatively short to avoid exceeding the Windows file path
    /// character limit.
    /// </summary>
    public class SrcMLFileNameMapping : ShortFileNameMapping {
        /// <summary>
        /// Creates a new ShortXmlFileNameMapping.
        /// </summary>
        /// <param name="targetDirectory">The directory for the target files.</param>
        public SrcMLFileNameMapping(string targetDirectory)
            : this(targetDirectory, ".xml") { }

        /// <summary>
        /// Creates a new ShortXmlFileNameMapping
        /// </summary>
        /// <param name="targetDirectory">The directory for the target files</param>
        /// <param name="targetExtension">the extension for the target files</param>
        public SrcMLFileNameMapping(string targetDirectory, string targetExtension)
            : base(targetDirectory, targetExtension) { }

        /// <summary>
        /// Gets the filename stored in the unit element of <paramref name="targetPath"/>
        /// </summary>
        /// <param name="targetPath">The target srcML file</param>
        /// <returns>The file name stored in <paramref name="targetPath"/></returns>
        protected override string GetSourcePathFromTargetFile(string targetPath) {
            try {
                var unit = XmlHelper.StreamElements(targetPath, SRC.Unit, 0).FirstOrDefault();
                return (null != unit ? SrcMLElement.GetFileNameForUnit(unit) : null);
            } catch(XmlException) {
                return null;
            }
            
        }
    }
}