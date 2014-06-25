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
    /// Maintains a mapping between source file paths and their corresponding target file paths.
    /// </summary>
    public abstract class AbstractFileNameMapping : IDisposable {
        private string _targetExtension;

        /// <summary>
        /// The directory where the target files should be located.
        /// </summary>
        public string TargetDirectory { get; protected set; }

        /// <summary>
        /// The extension to use for the target files. This will always have a starting period.
        /// </summary>
        public string TargetExtension {
            get { return _targetExtension; }
            protected set {
                if(!String.IsNullOrWhiteSpace(value) || '.' == value[0]) {
                    _targetExtension = value;
                } else {
                    _targetExtension = value.Insert(0, ".");
                }
            }
        }

        /// <summary>
        /// Creates a new AbstractFileNameMapping.
        /// </summary>
        /// <param name="targetDirectory">The directory for the XML files.</param>
        /// <param name="targetExtension">The target extension. If the extension has no starting period, one will be added.</param>
        protected AbstractFileNameMapping(string targetDirectory, string targetExtension) {
            if(string.IsNullOrWhiteSpace(targetDirectory)) {
                throw new ArgumentException("Argument cannot be null, empty, or whitespace.", "xmlDirectory");
            }
            this.TargetDirectory = Path.GetFullPath(targetDirectory);
            this.TargetExtension = targetExtension;
        }

        /// <summary>
        /// Returns all of the files 
        /// </summary>
        /// <returns>all of the files in <see cref="TargetDirectory"/> that have <see cref="TargetExtension"/></returns>
        public IEnumerable<string> GetTargetFiles() {
            return (Directory.Exists(TargetDirectory)
                    ? Directory.EnumerateFiles(TargetDirectory, String.Format("*{0}", TargetExtension), SearchOption.AllDirectories)
                    : Enumerable.Empty<string>());
        }

        /// <summary>
        /// Returns the path for the XML file mapped to <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="sourcePath">The path for the source file.</param>
        /// <returns>The full path for an XML file based on <paramref name="sourcePath"/>.</returns>
        public abstract string GetTargetPath(string sourcePath);

        /// <summary>
        /// Returns the path where the source file for <paramref name="targetPath"/> is located.
        /// </summary>
        /// <param name="targetPath">The path for the XML file.</param>
        /// <returns>The full path for the source file that <paramref name="targetPath"/> is based on.</returns>
        public abstract string GetSourcePath(string targetPath);

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
