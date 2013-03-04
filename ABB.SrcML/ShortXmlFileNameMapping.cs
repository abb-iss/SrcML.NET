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
    /// The names of the XML files are relatively short to avoid exceeding the Windows file path character limit.
    /// </summary>
    public class ShortXmlFileNameMapping : XmlFileNameMapping {
        private const string mappingFile = "mapping.txt";
        private readonly object mappingLock = new object();
        //maps source path to xml path
        private Dictionary<string, string> mapping;
        //maps source files names (without path) to a count of how many times that name has been seen
        private Dictionary<string, int> nameCount;
        
        /// <summary>
        /// Creates a new ShortXmlFileNameMapping.
        /// </summary>
        /// <param name="xmlDirectory">The directory for the XML files.</param>
        public ShortXmlFileNameMapping(string xmlDirectory)
            : base(xmlDirectory) {
            if(CheckIfDirectoryIsCaseInsensitive(xmlDirectory)) {
                mapping = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);
            } else {
                mapping = new Dictionary<string, string>();
            }
            nameCount = new Dictionary<string, int>();
            ReadMapping();
        }

        /// <summary>
        /// Returns the path for the XML file mapped to <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="sourcePath">The path for the source file.</param>
        /// <returns>The full path for an XML file based on <paramref name="sourcePath"/>.</returns>
        public override string GetXMLPath(string sourcePath) {
            if(string.IsNullOrWhiteSpace(sourcePath)) {
                throw new ArgumentException("Argument cannot be null, string.Empty, or whitespace.", "sourcePath");
            }
            
            sourcePath = Path.GetFullPath(sourcePath);
            string xmlPath;
            lock(mappingLock) {
                if(mapping.ContainsKey(sourcePath)) {
                    xmlPath = mapping[sourcePath];
                } else {
                    var sourceName = Path.GetFileName(sourcePath);
                    int newNameNum = nameCount.ContainsKey(sourceName) ? nameCount[sourceName] + 1 : 1;
                    nameCount[sourceName] = newNameNum;

                    xmlPath = Path.Combine(XmlDirectory, string.Format("{0}.{1}.xml", sourceName, newNameNum));
                    mapping[sourcePath] = xmlPath;
                }
            }
            return xmlPath;
        }

        /// <summary>
        /// Returns the path where the source file for <paramref name="xmlPath"/> is located.
        /// </summary>
        /// <param name="xmlPath">The path for the XML file.</param>
        /// <returns>The full path for the source file that <paramref name="xmlPath"/> is based on.</returns>
        public override string GetSourcePath(string xmlPath) {
            if(!Path.IsPathRooted(xmlPath)) {
                xmlPath = Path.Combine(XmlDirectory, xmlPath);
            }
            string result;
            lock(mappingLock) {
                if(mapping.ContainsValue(xmlPath)) {
                    var sourcePath = (from kvp in mapping
                                      where kvp.Value == xmlPath
                                      select kvp.Key);
                    result = sourcePath.FirstOrDefault();
                } else {
                    result = null;
                }
            }
            return result;
        }

        /// <summary>
        /// Saves the file name mapping to the XmlDirectory.
        /// </summary>
        public override void SaveMapping() {
            lock(mappingLock) {
                using(var outFile = new StreamWriter(Path.Combine(XmlDirectory, mappingFile))) {
                    foreach(var kvp in mapping) {
                        outFile.WriteLine(string.Format("{0}|{1}", kvp.Key, kvp.Value));
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of the object. This will write the mapping file to disk.
        /// </summary>
        public override void Dispose() {
            SaveMapping();
        }

        /// <summary>
        /// Reads the mapping file in XmlDirectory. 
        /// If this doesn't exist, it constructs a mapping from any existing SrcML files in the directory.
        /// </summary>
        protected void ReadMapping() {
            lock(mappingLock) {
                mapping.Clear();
                var mappingPath = Path.Combine(XmlDirectory, mappingFile);
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
                    Debug.WriteLine(string.Format("Mapping file not found: {0}", mappingPath));
                    if(Directory.Exists(XmlDirectory)) {
                        foreach(var xmlFile in Directory.GetFiles(XmlDirectory, "*.xml")) {
                            var unit = XmlHelper.StreamElements(xmlFile, SRC.Unit, 0).FirstOrDefault();
                            if(unit != null) {
                                //should be a SrcML file
                                var sourcePath = SrcMLElement.GetFileNameForUnit(unit);
                                if(!string.IsNullOrWhiteSpace(sourcePath)) {
                                    ProcessMapFileEntry(sourcePath, Path.GetFullPath(xmlFile));
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates the mapping data structures with the info from a single map file entry.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="xmlPath"></param>
        protected void ProcessMapFileEntry(string sourcePath, string xmlPath) {
            lock(mappingLock) {
                mapping[sourcePath] = xmlPath;
                //determine duplicate number
                var m = Regex.Match(xmlPath, @"\.(\d+)\.xml$");
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

        private bool CheckIfDirectoryIsCaseInsensitive(string directory) {
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
    }
}
