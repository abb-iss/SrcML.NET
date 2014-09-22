/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - Replaced Src2SrcMLRunner with Src2SrcMLRunner2
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML {
    /// <summary>
    /// The SrcML Generator class provides a convenient wrapper for multiple <see cref="Src2SrcMLRunner2">src2srcml runners</see>, each targetted at a different language.
    /// </summary>
    public class SrcMLGenerator : AbstractGenerator {
        private readonly Src2SrcMLRunner2 defaultExecutable;
        private readonly Language[] defaultLanguages = new[] { Language.C, Language.CPlusPlus, Language.Java, Language.AspectJ, Language.CSharp };
        private string[] defaultArguments;
        
        private Dictionary<Language, Src2SrcMLRunner2> nonDefaultExecutables;
        private Dictionary<Language, string[]> nonDefaultArguments;

        private readonly Dictionary<string, Language> extensionMapping = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase)
                                                                         {
                                                                             {".c", Language.C},
                                                                             {".h", Language.CPlusPlus},
                                                                             {".cpp", Language.CPlusPlus},
                                                                             {".cxx", Language.CPlusPlus},
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
        public Dictionary<Language, Src2SrcMLRunner2> NonDefaultExecutables {
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
        public SrcMLGenerator() : base() {
            defaultExecutable = new Src2SrcMLRunner2();
            defaultArguments = new[] { LIT.ArgumentLabel, OP.ArgumentLabel, POS.ArgumentLabel, TYPE.ArgumentLabel };
            nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner2>();
            nonDefaultArguments = new Dictionary<Language, string[]>();
            DetectNonDefaultExecutables();
        }

        /// <summary>
        /// Creates a new SrcMLGenerator
        /// </summary>
        /// <param name="defaultExecutableDirectory">The directory containing the default srcml executables to use.</param>
        public SrcMLGenerator(string defaultExecutableDirectory) : this(defaultExecutableDirectory, new[] { LIT.ArgumentLabel, OP.ArgumentLabel, POS.ArgumentLabel, TYPE.ArgumentLabel }) { }

        /// <summary>
        /// Creates a new SrcMLGenerator
        /// </summary>
        /// <param name="defaultExecutableDirectory">The directory containing the default srcml executables to use.</param>
        /// <param name="namespaceArguments">The namespace arguments to use when converting to SrcML.</param>
        public SrcMLGenerator(string defaultExecutableDirectory, IEnumerable<string> namespaceArguments) : base() {
            defaultExecutable = new Src2SrcMLRunner2(defaultExecutableDirectory);
            defaultArguments = namespaceArguments.ToArray();
            nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner2>();
            nonDefaultArguments = new Dictionary<Language, string[]>();
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
                nonDefaultExecutables = new Dictionary<Language, Src2SrcMLRunner2>();
            }

            var langList = languages.ToList();
            var dupLanguages = langList.Intersect(nonDefaultExecutables.Keys);
            if(dupLanguages.Any()) {
                var oldExec = nonDefaultExecutables[dupLanguages.First()];
                throw new InvalidOperationException(string.Format("Executable already registered for language {0}: {1}", dupLanguages.First(), oldExec.ExecutablePath));
            }

            var runner = new Src2SrcMLRunner2(executableDirectory);
            
            foreach(var lang in languages) {
                nonDefaultExecutables[lang] = runner;
                if(namespaceArguments != null) {
                    nonDefaultArguments[lang] = namespaceArguments.ToArray();
                }
            }
        }

        /// <summary>
        /// Scans the directory containing the default src2srcml executable and looks for subdirectories corresponding to defined languages.
        /// Each of these is registered for the given language.
        /// </summary>
        protected void DetectNonDefaultExecutables() {
            var defaultDir = new DirectoryInfo(defaultExecutable.ApplicationDirectory);
            if(defaultDir.Exists) {
                foreach(var dir in defaultDir.GetDirectories()) {
                    Language dirlanguage;
                    if(Enum.TryParse<Language>(dir.Name, true, out dirlanguage)) {
                        if(File.Exists(Path.Combine(dir.FullName, Src2SrcMLRunner2.Src2SrcMLExecutableName))) {
                            RegisterExecutable(dir.FullName, new[] { dirlanguage }, defaultArguments);
                        }
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
        public void GenerateSrcMLFromFile(string sourceFileName, string xmlFileName) {
            GenerateSrcMLFromFile(sourceFileName, xmlFileName, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from a single source file.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as. If this is Language.Any, then the language will be determined from the file extension.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public void GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, Language language) {
            if(language == Language.Any) {
                string ext = Path.GetExtension(sourceFileName);
                if(ext == null || !extensionMapping.ContainsKey(ext)) {
                    throw new ArgumentException(string.Format("Unknown file extension: {0}", ext), "sourceFileName");
                }
                language = extensionMapping[ext];
            }
            Src2SrcMLRunner2 runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            var additionalArguments = CreateArgumentsForLanguage(language);
            var runnerExtMap = CreateExtensionMappingForRunner(runner);

            runner.GenerateSrcMLFromFile(sourceFileName, xmlFileName, language, additionalArguments, runnerExtMap);
        }

        public void GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName) {
            var filesByLanguage = new Dictionary<Language, List<string>>();
            //determine which runner should process each source file
            foreach(var sourceFile in sourceFileNames) {
                var ext = Path.GetExtension(sourceFile);
                if(ext != null && extensionMapping.ContainsKey(ext)) {
                    Language lang = extensionMapping[ext];
                    if(nonDefaultExecutables.ContainsKey(lang)) {
                        //this file should be parsed by a non-default runner
                        if(!filesByLanguage.ContainsKey(lang)) {
                            filesByLanguage[lang] = new List<string>() { sourceFile };
                        } else {
                            filesByLanguage[lang].Add(sourceFile);
                        }
                    } else {
                        //should be parsed by the default runner
                        if(!filesByLanguage.ContainsKey(Language.Any)) {
                            filesByLanguage[Language.Any] = new List<string>() { sourceFile };
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
                    var mapForRunner = CreateExtensionMappingForRunner(defaultExecutable);
                    defaultExecutable.GenerateSrcMLFromFiles(kvp.Value, tempOutputFile, Language.Any, new Collection<string>(defaultArguments.ToList()), mapForRunner);
                } else {
                    var runner = nonDefaultExecutables[kvp.Key];
                    var mapForRunner = CreateExtensionMappingForRunner(runner);
                    runner.GenerateSrcMLFromFiles(kvp.Value, tempOutputFile, kvp.Key, new Collection<string>(nonDefaultArguments[kvp.Key].ToList()), mapForRunner);
                }
                tempResult = new SrcMLFile(tempOutputFile);
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
        }
        /// <summary>
        /// Generates a SrcML document from a collection of source files. The language(s) will be inferred from the file extensions.
        /// </summary>
        /// <param name="sourceFileNames">The source files to generate SrcML from.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFileFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName) {
            GenerateSrcMLFromFiles(sourceFileNames, xmlFileName);
            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generates a SrcML document from a collection of source files using the specified language.
        /// </summary>
        /// <param name="sourceFileNames">The source files to generate SrcML from.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source files as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public void GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName, Language language) {
            Src2SrcMLRunner2 runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            var mapForRunner = CreateExtensionMappingForRunner(runner);
            var additionalArguments = CreateArgumentsForLanguage(language);

            runner.GenerateSrcMLFromFiles(sourceFileNames, xmlFileName, language, additionalArguments, mapForRunner);
        }

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFileFromDirectory(string directoryPath, string xmlFileName) {
            return GenerateSrcMLFileFromDirectory(directoryPath, xmlFileName, Language.Any);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files not present in <paramref name="filesToExclude"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/>.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFileFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude) {
            return GenerateSrcMLFileFromDirectory(directoryPath, xmlFileName, filesToExclude, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file. The file will only contain source files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="languageFilter">The language to include.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFileFromDirectory(string directoryPath, string xmlFileName, Language languageFilter) {
            return GenerateSrcMLFileFromDirectory(directoryPath, xmlFileName, new string[] { }, languageFilter);
        }

        public SrcMLFile GenerateSrcMLFileFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter) {
            GenerateSrcMLFromDirectory(directoryPath, xmlFileName, filesToExclude, languageFilter);
            return new SrcMLFile(xmlFileName);
        }

        public void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName) {

        }

        public void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Language languageFilter) {
        }

        public void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude) {

        }
        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file. The file will only contain source files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="xmlFileName">The path of the xml file.</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/>.</param>
        /// <param name="languageFilter">The language to include.</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/>.</returns>
        public void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter) {
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
            GenerateSrcMLFileFromFiles(reducedFileList, xmlFileName);
        }

        /// <summary>
        /// Generate SrcML from a given string of source code. The source code will be parsed as C++.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source) {
            return defaultExecutable.GenerateSrcMLFromString(source, Language.CPlusPlus, new Collection<string>(defaultArguments), true);
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
            Src2SrcMLRunner2 runner = nonDefaultExecutables.ContainsKey(language) ? nonDefaultExecutables[language] : defaultExecutable;
            var additionalArguments = CreateArgumentsForLanguage(language);

            return runner.GenerateSrcMLFromString(source, language, additionalArguments, true);
        }

        private Collection<string> CreateArgumentsForLanguage(Language language) {
            return new Collection<string>(nonDefaultArguments.ContainsKey(language) ? nonDefaultArguments[language] : defaultArguments);
        }

        private Dictionary<string, Language> CreateExtensionMappingForRunner(Src2SrcMLRunner2 runner) {
            Dictionary<string, Language> extensionMapForRunner = new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<KeyValuePair<string, Language>> kvps = Enumerable.Empty<KeyValuePair<string,Language>>();
            if(runner == defaultExecutable) {
                kvps = from kvp in ExtensionMapping
                       where defaultLanguages.Contains(kvp.Value)
                       select kvp;
            } else if(nonDefaultExecutables.Values.Contains(runner)) {
                var registeredLanguages = (from kvp in nonDefaultExecutables
                                           where kvp.Value == runner
                                           select kvp.Key).ToList();
                kvps = from kvp in ExtensionMapping
                       where registeredLanguages.Contains(kvp.Value)
                       select kvp;
            }
            foreach(var kvp in kvps) {
                extensionMapForRunner[kvp.Key] = kvp.Value;
            }
            
            return extensionMapForRunner;
        }

        public override ICollection<string> SupportedExtensions {
            get { return ExtensionMapping.Keys; }
        }

        protected override bool GenerateImpl(string inputFileName, string outputFileName) {
            GenerateSrcMLFromFile(inputFileName, outputFileName);
            return true;
        }
    }
}
