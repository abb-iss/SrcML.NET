/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML {
    public class SrcMLGenerator {
        private readonly Src2SrcMLRunner defaultExecutable;
        private readonly Language[] defaultLanguages = new[] {Language.C, Language.CPlusPlus, Language.Java};
        private Dictionary<Language, Src2SrcMLRunner> nonDefaultExecutables;

        private readonly Dictionary<string, Language> extensionMapping = new Dictionary<string, Language>(StringComparer.InvariantCultureIgnoreCase)
                                                                         {
                                                                             {".c", Language.C},
                                                                             {".h", Language.CPlusPlus},
                                                                             {".cpp", Language.CPlusPlus},
                                                                             {".java", Language.Java},
                                                                             {".cs", Language.CSharp}
                                                                         };

        /// <summary>
        /// Maps file extensions to the languages they will be parsed as.
        /// </summary>
        public Dictionary<string, Language> ExtensionMapping {
            get { return extensionMapping; }
        }

        /// <summary>
        /// Maps Languages to the Src2SrcMLRunner that will parse it, if different from the default.
        /// </summary>
        public Dictionary<Language, Src2SrcMLRunner> NonDefaultExecutables {
            get { return nonDefaultExecutables; }
        }

        /// <summary>
        /// The languages that can be parsed by this SrcMLGenerator.
        /// </summary>
        public IEnumerable<Language> SupportedLanguages {
            get { return defaultLanguages.Union(nonDefaultExecutables.Keys); }
        } 

        /// <summary>
        /// Creates a new SrcMLGenerator.
        /// </summary>
        public SrcMLGenerator() {
            defaultExecutable = new Src2SrcMLRunner();
            nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner>();
            DetectNonDefaultExecutables();
        }

        /// <summary>
        /// Creates a new SrcMLGenerator
        /// </summary>
        /// <param name="defaultExecutableDirectory">The directory containing the default srcml executables to use.</param>
        public SrcMLGenerator(string defaultExecutableDirectory) {
            defaultExecutable = new Src2SrcMLRunner(defaultExecutableDirectory);
            nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner>();
            DetectNonDefaultExecutables();
        }

        /// <summary>
        /// Creates a new SrcMLGenerator
        /// </summary>
        /// <param name="defaultExecutableDirectory">The directory containing the default srcml executables to use.</param>
        /// <param name="namespaceArguments">The namespace arguments to use when converting to SrcML.</param>
        public SrcMLGenerator(string defaultExecutableDirectory, IEnumerable<string> namespaceArguments) {
            defaultExecutable = new Src2SrcMLRunner(defaultExecutableDirectory, namespaceArguments);
            nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner>();
            DetectNonDefaultExecutables();
        }

        /// <summary>
        /// Registers a src2srcml executable to use for the given languages.
        /// </summary>
        /// <param name="executableDirectory">The directory containing the src2srcml executable to use.</param>
        /// <param name="languages">A collection of the Languages that should be parsed by this executable.</param>
        public void RegisterExecutable(string executableDirectory, IEnumerable<Language> languages) {
            RegisterExecutable(executableDirectory, languages, null);
        }

        /// <summary>
        /// Registers a src2srcml executable to use for the given languages.
        /// </summary>
        /// <param name="executableDirectory">The directory containing the src2srcml executable to use.</param>
        /// <param name="languages">A collection of the Languages that should be parsed by this executable.</param>
        /// <param name="namespaceArguments">The namespace arguments to use when converting to SrcML.</param>
        public void RegisterExecutable(string executableDirectory, IEnumerable<Language> languages, IEnumerable<string> namespaceArguments) {
            if(nonDefaultExecutables == null) {
                nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner>();
            }

            var langList = languages.ToList();
            var dupLanguages = langList.Intersect(nonDefaultExecutables.Keys);
            if(dupLanguages.Any()) {
                var oldExec = nonDefaultExecutables[dupLanguages.First()];
                throw new InvalidOperationException(string.Format("Executable already registered for language {0}: {1}", dupLanguages.First(), oldExec.ExecutablePath));
            }

            var runner = namespaceArguments != null ? new Src2SrcMLRunner(executableDirectory, namespaceArguments) : new Src2SrcMLRunner(executableDirectory);
            foreach(var lang in languages) {
                nonDefaultExecutables[lang] = runner;
            }
        }

        /// <summary>
        /// Scans the directory containing the default src2srcml executable and looks for subdirectories corresponding to defined languages.
        /// Each of these is registered for the given language.
        /// </summary>
        protected void DetectNonDefaultExecutables() {
            var defaultDir = new DirectoryInfo(defaultExecutable.ApplicationDirectory);
            foreach(var dir in defaultDir.GetDirectories()) {
                Language dirlanguage;
                if(Enum.TryParse<Language>(dir.Name, true, out dirlanguage)) {
                    if(File.Exists(Path.Combine(dir.FullName, Src2SrcMLRunner.Src2SrcMLExecutableName))) {
                        RegisterExecutable(dir.FullName, new[] {dirlanguage});
                    }
                }
            }
        }

        /// <summary>
        /// Generate a SrcML document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">The path of the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName) {
            string ext = Path.GetExtension(sourceFileName);
            if(ext == null || !extensionMapping.ContainsKey(ext)) {
                throw new ArgumentException(string.Format("Unknown file extension: {0}", ext), "sourceFileName");
            }
            return GenerateSrcMLFromFile(sourceFileName, xmlFileName, extensionMapping[ext]);
        }

        /// <summary>
        /// Generate a SrcML document from a single source file with the specified language.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, Language language) {
            Src2SrcMLRunner runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            SetExtensionMappingOnRunner(runner);
            return runner.GenerateSrcMLFromFile(sourceFileName, xmlFileName, language);
        }

        //TODO: should this method be here? Can we get rid of it altogether?
        /// <summary>
        /// Generate a SrcML document from a single source file, and return an XElement for it. 
        /// The source language will be inferred from the file extension.
        /// </summary>
        /// <param name="sourceFileName">The path of the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>The root XElement for <paramref name="xmlFileName"/>.</returns>
        public XElement GenerateSrcMLAndXElementFromFile(string sourceFileName, string xmlFileName) {
            SrcMLFile file = GenerateSrcMLFromFile(sourceFileName, xmlFileName);
            return file.FileUnits.FirstOrDefault();
        }

        //TODO: should this method be here? Can we get rid of it altogether?
        /// <summary>
        /// Generate a SrcML document from a single source file, and return the xml as a string.
        /// The source language will be inferred from the file extension.
        /// </summary>
        /// <param name="sourceFileName">The path of the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>A string containing the xml for the file.</returns>
        public string GenerateSrcMLAndStringFromFile(string sourceFileName, string xmlFileName) {
            SrcMLFile file = GenerateSrcMLFromFile(sourceFileName, xmlFileName);
            return file.GetXMLString();
        }

        /// <summary>
        /// Generates a SrcML document from a collection of source files. The language(s) will be inferred from the file extensions.
        /// </summary>
        /// <param name="sourceFileNames">The source files to generate SrcML from.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName) {
            var filesByLanguage = new Dictionary<Language, List<string>>();
            //determine which runner should process each source file
            foreach(var sourceFile in sourceFileNames) {
                var ext = Path.GetExtension(sourceFile);
                if(ext != null && extensionMapping.ContainsKey(ext)) {
                    Language lang = extensionMapping[ext];
                    if(nonDefaultExecutables.ContainsKey(lang)) {
                        //this file should be parsed by a non-default runner
                        if(!filesByLanguage.ContainsKey(lang)) {
                            filesByLanguage[lang] = new List<string>() {sourceFile};
                        } else {
                            filesByLanguage[lang].Add(sourceFile);
                        }
                    } else {
                        //should be parsed by the default runner
                        if(!filesByLanguage.ContainsKey(Language.Any)) {
                            filesByLanguage[Language.Any] = new List<string>() {sourceFile};
                        } else {
                            filesByLanguage[Language.Any].Add(sourceFile);
                        }
                    }
                }
            }

            //convert files to srcml
            SrcMLFile tempArchive = null;
            foreach(var kvp in filesByLanguage) {
                var tempOutputFile = Path.GetTempFileName();
                SrcMLFile tempResult;
                if(kvp.Key == Language.Any) {
                    SetExtensionMappingOnRunner(defaultExecutable);
                    tempResult = defaultExecutable.GenerateSrcMLFromFiles(kvp.Value, tempOutputFile);
                } else {
                    var runner = nonDefaultExecutables[kvp.Key];
                    SetExtensionMappingOnRunner(runner);
                    tempResult = runner.GenerateSrcMLFromFiles(kvp.Value, tempOutputFile, kvp.Key);
                }

                var oldArchiveFile = tempArchive != null ? tempArchive.FileName : null;
                tempArchive = tempResult.Merge(tempArchive, Path.GetTempFileName());
                File.Delete(tempOutputFile);
                if(oldArchiveFile != null) { File.Delete(oldArchiveFile); }
            }

            if(tempArchive != null) {
                if(File.Exists(xmlFileName)) {
                    File.Delete(xmlFileName);
                }
                File.Move(tempArchive.FileName, xmlFileName);
            }
            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generates a SrcML document from a collection of source files using the specified language.
        /// </summary>
        /// <param name="sourceFileNames">The source files to generate SrcML from.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source files as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName, Language language) {
            Src2SrcMLRunner runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            SetExtensionMappingOnRunner(runner);
            return runner.GenerateSrcMLFromFiles(sourceFileNames, xmlFileName, language);
        }

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName) {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, Language.Any);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files not present in <paramref name="filesToExclude"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/>.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude) {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, filesToExclude, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file. The file will only contain source files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="languageFilter">The language to include.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Language languageFilter) {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, new string[] {}, languageFilter);
        }

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file. The file will only contain source files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/>.</param>
        /// <param name="languageFilter">The language to include.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter) {
            if(!Directory.Exists(directoryPath)) {
                throw new DirectoryNotFoundException(String.Format("{0} does not exist.", directoryPath));
            }

            var dir = new DirectoryInfo(directoryPath);
            var fileObjectsToExclude = from f in filesToExclude
                                       select new FileInfo(f);
            var files = (from filePath in dir.GetFiles("*", SearchOption.AllDirectories)
                         where extensionMapping.ContainsKey(filePath.Extension)
                         select filePath).Except(fileObjectsToExclude, new FileInfoComparer());

            IEnumerable<string> reducedFileList;
            if(languageFilter == Language.Any) {
                reducedFileList = from f in files
                                  select f.FullName;
            } else {
                reducedFileList = from f in files
                                  where extensionMapping.ContainsKey(f.Extension) && extensionMapping[f.Extension] == languageFilter
                                  select f.FullName;
            }

            return GenerateSrcMLFromFiles(reducedFileList, xmlFileName);
        }

        /// <summary>
        /// Generate SrcML from a given string of source code. The source code will be parsed as C++.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source) {
            return defaultExecutable.GenerateSrcMLFromString(source);
        }

        /// <summary>
        /// Generate SrcML from a given string of source code.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <param name="language">The language to parse the string as. Language.Any is not valid.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source, Language language) {
            if(language == Language.Any) {
                throw new SrcMLException("Any is not a valid language. Pick an actual language in the enumeration");
            }
            Src2SrcMLRunner runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            return runner.GenerateSrcMLFromString(source, language);
        }

        


        private void SetExtensionMappingOnRunner(Src2SrcMLRunner runner) {
            if(runner == defaultExecutable) {
                var runnerMap = runner.ExtensionMapping;
                runnerMap.Clear();
                foreach(var kvp in extensionMapping) {
                    if(defaultLanguages.Contains(kvp.Value)) {
                        runnerMap[kvp.Key] = kvp.Value;
                    }
                }
            } else if(nonDefaultExecutables.Values.Contains(runner)) {
                var runnerMap = runner.ExtensionMapping;
                runnerMap.Clear();
                var registeredLanguages = (from kvp in nonDefaultExecutables
                                           where kvp.Value == runner
                                           select kvp.Key).ToList();
                foreach(var kvp in extensionMapping) {
                    if(registeredLanguages.Contains(kvp.Value)) {
                        runnerMap[kvp.Key] = kvp.Value;
                    }
                }
            } else {
                throw new ArgumentException("Unregistered Src2SrcMLRunner", "runner");
            }
        }
    }
}
