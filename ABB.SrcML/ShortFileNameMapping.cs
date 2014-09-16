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

namespace ABB.SrcML {
    /// <summary>
    /// Maintains a mapping between source file paths and the paths where XML versions are stored.
    /// The names of the XML files are relatively short to avoid exceeding the Windows file path
    /// character limit.
    /// </summary>
    public class ShortFileNameMapping : AbstractFileNameMapping {
        private readonly object mappingLock = new object();
        private Dictionary<string, string> mapping;
        private Dictionary<string, string> reverseMapping;
        private Dictionary<string, int> nameCount;
        private volatile bool _changed;
        /// <summary>
        /// The name to use to save this mapping to disk
        /// </summary>
        protected const string mappingFile = "mapping.txt";

        /// <summary>
        /// Creates a new abstract short file name mapping
        /// </summary>
        /// <param name="targetDirectory">The target directory to store the mapped files in</param>
        /// <param name="targetExtension">The target extension</param>
        protected ShortFileNameMapping(string targetDirectory, string targetExtension) 
        : base(targetDirectory, targetExtension) {
            bool directoryIsCaseInsensitive = CheckIfDirectoryIsCaseInsensitive(targetDirectory);
            mapping = new Dictionary<string, string>(directoryIsCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            reverseMapping = new Dictionary<string, string>(directoryIsCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            nameCount = new Dictionary<string, int>(directoryIsCaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            ReadMapping();
        }
        
        /// <summary>
        /// Gets the target path for <paramref name="sourcePath" />.
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <returns>The target path</returns>
        public override string GetTargetPath(string sourcePath) {
            if(string.IsNullOrWhiteSpace(sourcePath)) {
                throw new ArgumentException("Argument cannot be null, string.Empty, or whitespace.", "sourcePath");
            }

            sourcePath = Path.GetFullPath(sourcePath);
            string targetPath;
            lock(mappingLock) {
                if(mapping.ContainsKey(sourcePath)) {
                    targetPath = mapping[sourcePath];
                } else {
                    var sourceName = Path.GetFileName(sourcePath);
                    int newNameNum = nameCount.ContainsKey(sourceName) ? nameCount[sourceName] + 1 : 1;
                    nameCount[sourceName] = newNameNum;

                    targetPath = Path.Combine(TargetDirectory, string.Format("{0}.{1}{2}", sourceName, newNameNum, TargetExtension));
                    mapping[sourcePath] = targetPath;
                    reverseMapping[targetPath] = sourcePath;
                    _changed = true;
                }
            }
            return targetPath;
        }

        /// <summary>
        /// Returns the source path for a give target path.
        /// </summary>
        /// <param name="targetPath">The target path</param>
        /// <returns>The corresponding source path for <paramref name="targetPath"/>. If <paramref name="targetPath"/> is not in the mapping, null is returned.</returns>
        public override string GetSourcePath(string targetPath) {
            if(!Path.IsPathRooted(targetPath)) {
                targetPath = Path.Combine(TargetDirectory, targetPath);
            }

            lock(mappingLock) {
                string result;
                return (reverseMapping.TryGetValue(targetPath, out result) ? result : null);
            }
        }

        /// <summary>
        /// Updates the mapping data structures with the info from a single map file entry.
        /// </summary>
        /// <param name="sourcePath">The source path</param>
        /// <param name="targetPath">The target path</param>
        protected void ProcessMapFileEntry(string sourcePath, string targetPath) {
            lock(mappingLock) {
                mapping[sourcePath] = targetPath;
                reverseMapping[targetPath] = sourcePath;
                //determine duplicate number
                var m = Regex.Match(targetPath, String.Format(@"\.(\d+)\{0}$", TargetExtension));
                if(m.Success) {
                    var sourceName = Path.GetFileName(sourcePath);
                    var nameNum = int.Parse(m.Groups[1].Value);
                    var currMax = -1;
                    if(nameCount.ContainsKey(sourceName)) {
                        currMax = nameCount[sourceName];
                    }
                    nameCount[sourceName] = nameNum > currMax ? nameNum : currMax;
                }
            }
        }

        /// <summary>
        /// Gets the source path from a target file. This may require reading the file to find out what the source path is.
        /// </summary>
        /// <param name="targetPath">The target path</param>
        /// <returns>The source path found in <paramref name="targetPath"/>. If the source path can't be found in the target file, then null is returned.</returns>
        protected virtual string GetSourcePathFromTargetFile(string targetPath) {
            return null;
        }

        /// <summary>
        /// Reads the mapping file in XmlDirectory. If this doesn't exist, it constructs a mapping
        /// from any existing SrcML files in the directory.
        /// </summary>
        protected void ReadMapping() {
            lock(mappingLock) {
                mapping.Clear();
                var mappingPath = Path.Combine(TargetDirectory, mappingFile);
                if(File.Exists(mappingPath)) {
                    //read mapping file
                    foreach(var line in File.ReadLines(mappingPath)) {
                        var paths = line.Split('|');
                        if(paths.Length != 2) {
                            Debug.WriteLine(string.Format("Bad line found in mapping file. Expected 2 fields, has {0}: {1}", paths.Length, line));
                            continue;
                        }
                        ProcessMapFileEntry(paths[0].Trim(), paths[1].Trim());
                    }
                    //TODO: remove file from disk
                } else {
                    //mapping file doesn't exist, so construct mapping from the xml files in the directory
                    if(Directory.Exists(TargetDirectory)) {
                        foreach(var targetFile in Directory.GetFiles(TargetDirectory, String.Format("*{0}", TargetExtension))) {
                            var sourcePath = GetSourcePathFromTargetFile(targetFile);
                            if(!string.IsNullOrWhiteSpace(sourcePath)) {
                                ProcessMapFileEntry(sourcePath, Path.GetFullPath(targetFile));
                            } else {
                                File.Delete(targetFile);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves the mapping to disk
        /// </summary>
        public override void SaveMapping() {
            if(_changed) {
                lock(mappingLock) {
                    using(var outFile = new StreamWriter(Path.Combine(TargetDirectory, mappingFile))) {
                        foreach(var kvp in mapping) {
                            outFile.WriteLine(string.Format("{0}|{1}", kvp.Key, kvp.Value));
                        }
                    }
                }
                _changed = false;
            }
        }

        private static bool CheckIfDirectoryIsCaseInsensitive(string directory) {
            bool isCaseInsensitive = false;
            string tempFile = string.Empty;
            try {
                if(Directory.Exists(directory)) {
                    tempFile = Path.Combine(directory, Guid.NewGuid().ToString()).ToLower();
                } else {
                    tempFile = Path.GetTempFileName().ToLower();
                }
                File.Create(tempFile).Close();
                isCaseInsensitive = File.Exists(tempFile.ToUpper());
            } finally {
                if(File.Exists(tempFile)) {
                    File.Delete(tempFile);
                }
            }
            return isCaseInsensitive;
        }

        /// <summary>
        /// Disposes of this mapping object. It first calls <see cref="SaveMapping"/>
        /// </summary>
        public override void Dispose() {
            SaveMapping();
        }
    }
}
