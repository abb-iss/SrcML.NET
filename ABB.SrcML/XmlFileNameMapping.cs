/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    /// <summary>
    /// Maintains a mapping between source file paths and the paths where XML versions are stored.
    /// </summary>
    public abstract class XmlFileNameMapping : IDisposable {
        /// <summary>
        /// The directory where the XML files should be located.
        /// </summary>
        public string XmlDirectory { get; protected set; }

        /// <summary>
        /// Creates a new XmlFileNameMapping.
        /// </summary>
        /// <param name="xmlDirectory">The directory for the XML files.</param>
        protected XmlFileNameMapping(string xmlDirectory) {
            if(string.IsNullOrWhiteSpace(xmlDirectory)) {
                throw new ArgumentException("Argument cannot be null, empty, or whitespace.", "xmlDirectory");
            }
            this.XmlDirectory = Path.GetFullPath(xmlDirectory);
        }

        /// <summary>
        /// Returns the path for the XML file mapped to <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="sourcePath">The path for the source file.</param>
        /// <returns>The full path for an XML file based on <paramref name="sourcePath"/>.</returns>
        public abstract string GetXmlPath(string sourcePath);

        /// <summary>
        /// Returns the path where the source file for <paramref name="xmlPath"/> is located.
        /// </summary>
        /// <param name="xmlPath">The path for the XML file.</param>
        /// <returns>The full path for the source file that <paramref name="xmlPath"/> is based on.</returns>
        public abstract string GetSourcePath(string xmlPath);

        /// <summary>
        /// Saves the file name mapping to the XmlDirectory.
        /// </summary>
        public abstract void SaveMapping();

        #region IDisposable Members

        /// <summary>
        /// Disposes of this file name mapping
        /// </summary>
        public abstract void Dispose();

        #endregion
    }
}
