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
    public class XmlFileNameMapping : IDisposable {
        /// <summary>
        /// The directory where the XML files should be located.
        /// </summary>
        public string XmlDirectory { get; protected set; }

        /// <summary>
        /// Creates a new XmlFileNameMapping.
        /// </summary>
        /// <param name="xmlDirectory">The directory for the XML files.</param>
        public XmlFileNameMapping(string xmlDirectory) {
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
        public virtual string GetXMLPath(string sourcePath) {
            string srcMLFileName = Path.GetFullPath(sourcePath).Replace("\\", "-").Replace(":", "=");   // Simple encoding
            string xmlPath = Path.Combine(XmlDirectory, srcMLFileName) + ".xml";
            return xmlPath;
        }

        /// <summary>
        /// Returns the path where the source file for <paramref name="xmlPath"/> is located.
        /// </summary>
        /// <param name="xmlPath">The path for the XML file.</param>
        /// <returns>The full path for the source file that <paramref name="xmlPath"/> is based on.</returns>
        public virtual string GetSourcePath(string xmlPath) {
            var sourcePath = Path.GetFileNameWithoutExtension(xmlPath);
            if(sourcePath != null) {
                sourcePath = sourcePath.Replace("=", ":").Replace("-", "\\"); // Simple decoding
            }
            return sourcePath;
        }

        /// <summary>
        /// Saves the file name mapping to the XmlDirectory.
        /// </summary>
        public virtual void SaveMapping() {
            
        }

        #region IDisposable Members

        public virtual void Dispose() {
            
        }

        #endregion
    }
}
