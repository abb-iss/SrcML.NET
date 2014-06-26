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
    public class DataFileNameMapping : ShortFileNameMapping {
        private bool _compressionEnabled;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetDirectory"></param>
        public DataFileNameMapping(string targetDirectory)
            : this(targetDirectory, true) { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetDirectory"></param>
        /// <param name="compressionEnabled"></param>
        public DataFileNameMapping(string targetDirectory, bool compressionEnabled)
            : base(targetDirectory, GetDefaultExtension(compressionEnabled)) {
            _compressionEnabled = compressionEnabled;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        protected override string GetSourcePathFromTargetFile(string targetPath) {
            string sourcePath = null;
            var targetExtension = Path.GetExtension(targetPath);
            bool targetNeedsDecompression = String.IsNullOrWhiteSpace(targetExtension) ||
                                            targetExtension.Equals(XmlSerialization.DEFAULT_COMPRESSED_EXTENSION,
                                                                   StringComparison.InvariantCultureIgnoreCase);

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
