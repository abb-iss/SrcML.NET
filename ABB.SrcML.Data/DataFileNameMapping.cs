/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - Initial implementation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// File name mapping for data files
    /// </summary>
    public class DataFileNameMapping : ShortFileNameMapping {
        private bool _compressionEnabled;

        /// <summary>
        /// Create a new data file name mapping. This uses <see cref="XmlSerialization.DEFAULT_COMPRESSED_EXTENSION"/>
        /// as the extension for all target files.
        /// </summary>
        /// <param name="targetDirectory">The directory for the target files</param>
        public DataFileNameMapping(string targetDirectory)
            : this(targetDirectory, true) { }

        /// <summary>
        /// Creates a new data file name mapping
        /// </summary>
        /// <param name="targetDirectory">The directory for the target files</param>
        /// <param name="compressionEnabled">
        /// If true, use <see cref="XmlSerialization.DEFAULT_COMPRESSED_EXTENSION">the default compression extension</see>.
        /// Otherwise, use <see cref="XmlSerialization.DEFAULT_EXTENSION">the default extension</see></param>.
        public DataFileNameMapping(string targetDirectory, bool compressionEnabled)
            : base(targetDirectory, GetDefaultExtension(compressionEnabled)) {
            _compressionEnabled = compressionEnabled;
        }

        /// <summary>
        /// Reads the source path from the <paramref name="targetPath"/>.
        /// This works by reading the XML and finding the first <see cref="SrcMLLocation.XmlName">Location</see> element.
        /// It then gets the <see cref="SourceLocation.XmlFileAttributeName">file attribute</see> within it. If the file
        /// is compressed, it should automatically decompress it.
        /// </summary>
        /// <param name="targetPath">The target path</param>
        /// <returns>The source path found in <paramref name="targetPath"/></returns>
        protected override string GetSourcePathFromTargetFile(string targetPath) {
            string sourcePath = null;
            var targetExtension = Path.GetExtension(targetPath);
            bool targetNeedsDecompression = String.IsNullOrWhiteSpace(targetExtension) ||
                                            targetExtension.Equals(XmlSerialization.DEFAULT_COMPRESSED_EXTENSION,
                                                                   StringComparison.OrdinalIgnoreCase);

            using(var fileStream = File.OpenRead(targetPath)) {
                if(targetNeedsDecompression) {
                    using(var zipStream = new GZipStream(fileStream, CompressionMode.Decompress)) {
                        sourcePath = GetSourcePathFromTarget(zipStream);
                    }
                } else {
                    sourcePath = GetSourcePathFromTarget(fileStream);
                }   
            }
            return sourcePath;
        }

        private static string GetDefaultExtension(bool compressionEnabled) {
            return compressionEnabled ? XmlSerialization.DEFAULT_COMPRESSED_EXTENSION : XmlSerialization.DEFAULT_EXTENSION;
        }

        private static string GetSourcePathFromTarget(Stream targetStream) {
            try {
                var target = XElement.Load(targetStream);
                var firstLocation = (from location in target.Descendants(SrcMLLocation.XmlName)
                                     select location.Attribute(SourceLocation.XmlFileAttributeName).Value).FirstOrDefault();
                return firstLocation;
            } catch(XmlException) {

            }
            return null;
        }
    }
}
