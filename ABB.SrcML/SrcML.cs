/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using ABB.SrcML.Utilities;
using System.Xml;
using System.Globalization;
using System.Security;

namespace ABB.SrcML
{
    /// <summary>
    /// This is a utility class for generating SrcML files. It has functions that use the original SrcML executables,
    /// and some native C# functions for generating SrcML.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1724:TypeNamesShouldNotMatchNamespaces")]
    public class SrcML
    {
        private readonly DefaultsDictionary<string, Language> _extensionMapping = new DefaultsDictionary<string, Language>(new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase) {
                    { ".c" , Language.C },
                    { ".h", Language.C },
                    { ".cpp", Language.CPlusPlus },
                    { ".java", Language.Java }
        });

        private readonly string src2srcml_exe;
        private readonly string srcml2src_exe;
        private readonly string srcmlDir;

        /// <summary>executable name for src2srcml executable</summary>
        public const string Src2SrcMLExecutableName = "src2srcml.exe";

        /// <summary>executable name for ExtractSourceFile executable</summary>
        public const string SrcML2SrcExecutableName = "srcml2src.exe";

        /// <summary>
        /// Mapping of source extensions to their languages.
        /// </summary>
        public DefaultsDictionary<string, Language> ExtensionMapping
        {
            get
            {
                return _extensionMapping;
            }
        }

        /// <summary>
        /// List of common name space arguments that src2srcml.exe uses to modify its output.
        /// </summary>
        public static Collection<string> DefaultNamespaceArguments
        {
            get
            {
                return new Collection<string> { "--literal", "--modifier", "--operator", "--position" };
            }
        }


        /// <summary>
        /// Creates a new SrcML object rooted in a default directory. If the SRCMLBINDIR environment variable is set, that is used.
        /// If not, then c:\Program Files (x86)\SrcML\bin is used.
        /// If that doesn't exist, c:\Program Files\SrcML\bin is used.
        /// 
        /// If none of these directories is sued, the current directory is used.
        /// <seealso cref="SrcML(string)"/>
        /// </summary>
        public SrcML() : this(SrcMLHelper.GetSrcMLDefaultDirectory())
        {
        }

        /// <summary>
        /// Gets the directory that src2srcml and ExtractSourceFile can be found in
        /// </summary>
        public string SrcMLDirectory
        {
            get { return this.srcmlDir; }
        }
        /// <summary>
        /// Creates a new SrcML object rooted in the given directory.
        /// </summary>
        /// <param name="binDirectory">The path to the directory containing the SrcML executables.</param>
        public SrcML(string binDirectory)
        {
            this.srcmlDir = binDirectory;
            this.src2srcml_exe = Path.Combine(binDirectory, Src2SrcMLExecutableName);
            this.srcml2src_exe = Path.Combine(binDirectory, SrcML2SrcExecutableName);

            if(!File.Exists(this.src2srcml_exe))
                throw new FileNotFoundException(this.src2srcml_exe + " does not exist!");
            if (!File.Exists(this.srcml2src_exe))
                throw new FileNotFoundException(this.srcml2src_exe + " does not exist!");
        }

        /// <summary>
        /// Gets the default XmlNamespaceManager that contains all of the SrcML namespaces
        /// </summary>
        public static XmlNamespaceManager NamespaceManager
        {
            get
            {
                return SrcMLNamespaces.Manager;
            }
        }

#region Internal Code
        [SecurityCritical]
        private void generateSrcMLDoc(string rootDirectory, string xmlFileName, IEnumerable<string> fileNames)
        {
            var arguments = DefaultNamespaceArguments;

            var tempFileListing = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(tempFileListing))
            {
                foreach (var fileName in fileNames)
                {
                    writer.WriteLine(fileName);
                }
            }
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--files-from=\"{0}\"", tempFileListing));

            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--directory=\"{0}\"", rootDirectory));
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--output=\"{0}\"", xmlFileName));

            if (ExtensionMapping.NonDefaultValueCount > 0)
            {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--register-ext {0}", KsuAdapter.ConvertMappingToString(ExtensionMapping)));
            }

            var argumentString = KsuAdapter.MakeArgumentString(arguments);

            try
            {
                KsuAdapter.RunExecutable(this.src2srcml_exe, argumentString);
            }
            catch (SrcMLRuntimeException e)
            {
                throw new SrcMLException("src2srcml.exe encountered an error", e);
            }
            finally
            {
                File.Delete(tempFileListing);
            }
        }

        [SecurityCritical]
        private void generateSrcMLDoc(string path, string xmlFileName, Language language)
        {
            var arguments = DefaultNamespaceArguments;

            if (language > Language.Any)
            {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));
            }
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "\"{0}\"", path));
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--output=\"{0}\"", xmlFileName));

            var argumentString = KsuAdapter.MakeArgumentString(arguments);

            try
            {
                KsuAdapter.RunExecutable(this.src2srcml_exe, argumentString);
            }
            catch (SrcMLRuntimeException e)
            {
                throw new SrcMLException("src2srcml.exe encountered an error", e);
            }
        }
#endregion Internal Code

#region String Conversion
        /// <summary>
        /// Generate SrcML from a given string of source code. The source code will be parsed as C++.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source)
        {
            return GenerateSrcMLFromString(source, Language.CPlusPlus);
        }

        /// <summary>
        /// Generate SrcML from a given string of source code.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <param name="language">The source language to use (C,C++,Java,AspectJ).
        /// If the source languageFilter is either not in this list or is null, the default source language (C++) will be used.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source, Language language)
        {
            Collection<string> arguments = DefaultNamespaceArguments;
            arguments.Add("--no-xml-declaration");
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));

            var argumentString = KsuAdapter.MakeArgumentString(arguments);

            try
            {
                var xml = KsuAdapter.RunExecutable(this.src2srcml_exe, argumentString, source);
                return xml;
            }
            catch (SrcMLRuntimeException e)
            {
                throw new SrcMLException("src2srcml encountered an error", e);
            }
        }
#endregion

#region File Conversion
        /// <summary>
        /// Generate a SrcML document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName)
        {
            generateSrcMLDoc(sourceFileName, xmlFileName, Language.Any);

            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generate a SrcML document from a single source file with the specified language.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, Language language)
        {
            generateSrcMLDoc(sourceFileName, xmlFileName, language);

            return new SrcMLFile(xmlFileName);
        }
#endregion File Conversion

# region Project Conversion
        /// <summary>
        /// Generate a SrcML file for the given Visual Studio project. The resulting XML
        /// will be written to a file with the same name as the Visual Studio project
        /// <seealso cref="GenerateSrcMLFromProject(string, string)"/>
        /// </summary>
        /// <param name="project">The path to the Visual Studio project file.</param>
        /// <returns>A SrcMLFile based on the project.</returns>
        public SrcMLFile GenerateSrcMLFromProject(string project)
        {
            return GenerateSrcMLFromProject(project, Path.ChangeExtension(project, ".xml"));
        }

        /// <summary>
        /// Generate a SrcML file from the Visual Studio project file with the language C++.
        /// </summary>
        /// <param name="project">The path to the Visual Studo project file.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>a SrcMLFile for <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromProject(string project, string xmlFileName)
        {

            string[] srcfiles = VisualStudioProjectReader.ReadProjectFile(project);

            var directory = Path.GetDirectoryName(project);

            generateSrcMLDoc(directory, xmlFileName, srcfiles);
            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generate a SrcML file from the Visual Studio project file, with the given language.
        /// </summary>
        /// <param name="project">The path to the Visual Studo project file.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The source language to use (C,C++,Java,AspectJ)</param>
        /// <returns>a SrcMLFile for <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromProject(string project, string xmlFileName, Language language)
        {
            string[] srcfiles = VisualStudioProjectReader.ReadProjectFile(project);

            var sourceFiles = from sourceFile in srcfiles
                              let ext = Path.GetExtension(sourceFile)
                              where ext != null && ExtensionMapping[ext] == language
                              select sourceFile;

            var directory = Path.GetDirectoryName(project);

            generateSrcMLDoc(directory, xmlFileName, sourceFiles);
            return new SrcMLFile(xmlFileName);
        }
# endregion Project Conversion

#region Directory Conversion
        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file.
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName)
        {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, Enumerable.Empty<string>(), Language.Any);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <param name="languageFilter">the language to filter on</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Language languageFilter)
        {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, Enumerable.Empty<string>(), languageFilter);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files not present in <paramref name="filesToExclude"/>
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/></param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Collection<string> filesToExclude)
        {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, filesToExclude, Language.Any);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file.
        /// The output can be controlled by using <paramref name="filesToExclude"/>, and <paramref name="languageFilter"/>
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/></param>
        /// <param name="languageFilter">the language to filter on</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", directoryPath));

            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            var fileObjectsToExclude = from f in filesToExclude
                                       select new FileInfo(f);

            var files = (from filePath in dir.GetFiles("*", SearchOption.AllDirectories)
                         where ExtensionMapping.ContainsKey(filePath.Extension)
                         select filePath).Except(fileObjectsToExclude, new FileInfoComparer());

            IEnumerable<string> reducedFileList;
            if (Language.Any == languageFilter)
            {
                reducedFileList = from f in files select f.FullName;
            }
            else
            {
                reducedFileList = from f in files
                                  where languageFilter == ExtensionMapping[f.Extension]
                                  select f.FullName;
            }

            generateSrcMLDoc(dir.FullName, xmlFileName, reducedFileList);
            return new SrcMLFile(xmlFileName);
        }
#endregion Directory Conversion

#region srcml2src
        /// <summary>
        /// Runs the srcml2src.exe executable on the given SrcML document, and extracts the specific filename.
        /// <para>It's probably better to use the <see cref="Extensions.ToSource(XElement)"/> function, as it does not require starting a new process.</para>
        /// </summary>
        /// <param name="doc">The SrcML document to query.</param>
        /// <param name="unitIndex">The index number of the SrcML document.</param>
        /// <returns>The source code.</returns>
        /// TODO change this to use AbstractDocument and use the new srcml2src that comes with srcdiff (remove the suppressmessage after)
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2233:OperationsShouldNotOverflow", MessageId = "unitIndex+1",
                                                         Justification="ArgumentOutOfRangeException is thrown if unitIndex==int.MaxValue"), 
         System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public string ExtractSourceFile(SrcMLFile doc, int unitIndex)
        {
            if (null == doc)
                throw new ArgumentNullException("doc");
            if (unitIndex == int.MaxValue)
                throw new ArgumentOutOfRangeException("unitIndex", "Unit index must be less than Int32.MaxValue");
            
            string output, outfile = Path.GetTempFileName();
            string arguments = String.Format(CultureInfo.InvariantCulture, "-U {0} \"{1}\" \"{2}\"", unitIndex + 1, doc.FileName, outfile);

            try
            {
                KsuAdapter.RunExecutable(this.srcml2src_exe, arguments);
            }
            catch (SrcMLRuntimeException e)
            {
                throw new SrcMLException("srcml2src encountered an error", e);
            }

            using (StreamReader reader = new StreamReader(outfile))
            {
                output = reader.ReadToEnd();
            }

            File.Delete(outfile);

            return output;
        }

        /// <summary>
        /// Takes the given <paramref name="fileName"/> in <paramref name="doc"/> and returns the original source code.
        /// <para>Instead of this, use <see cref="Extensions.ToSource(XElement,int)"/>.</para>
        /// <seealso cref="ExtractSourceFile(SrcMLFile, int)"/>
        /// </summary>
        /// <param name="doc">The SrcML document to query.</param>
        /// <param name="fileName">The file name to search for.</param>
        /// <returns>A string with the original source code.</returns>
        public string ExtractSourceFile(SrcMLFile doc, string fileName)
        {
            if (null == doc)
                throw new ArgumentNullException("doc");

            return this.ExtractSourceFile(doc, doc.IndexOfUnit(fileName));
        }
#endregion srcml2src

#region Deprecated APIs
        /// <summary>
        /// Gets the parent statement for the given element.
        /// </summary>
        /// <param name="element">The element to find the parent of.</param>
        /// <returns>The parent element for <paramref name="element"/>. It will be either <see cref="SRC.ExpressionStatement"/> or <see cref="SRC.DeclarationStatement"/></returns>
        [Obsolete("use SRC.ParentStatement instead")]
        public static XElement getParentStatement(XElement element)
        {
            if (null == element)
                throw new ArgumentNullException("element");

            if (!element.Ancestors().Any())
                return null;
            return element.Ancestors().Where(a => a.Name == SRC.ExpressionStatement || a.Name == SRC.DeclarationStatement).First();
        }

        /// <summary>
        /// Generate SrcML from a given string of source code.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <param name="language">The source language to use (C,C++,Java,AspectJ).
        /// If the source langauge is either not in this list or is null, the default source language (C++) will be used.</param>
        /// <returns>XML representing the source.</returns>
        [Obsolete("Consider using GenerateSrcMLFromString(string,languageFilter)")]
        public string GenerateSrcMLFromString(string source, string language)
        {
            Language lang;
            if (null == language)
                lang = Language.CPlusPlus;
            else
                lang = SrcMLElement.GetLanguageFromString(language);

            return GenerateSrcMLFromString(source, lang);
        }
        /// <summary>
        /// Generate a SrcML document from a single source file with the specified language.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        [Obsolete("Consider using GenerateSrcMLFromFile(string,string,Language)")]
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, string language)
        {
            var lang = SrcMLElement.GetLanguageFromString(language);
            string fullPath = Path.GetFullPath(sourceFileName);

            generateSrcMLDoc(sourceFileName, xmlFileName, lang);

            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generate a SrcML file from the Visual Studio project file, with the given languageFilter.
        /// </summary>
        /// <param name="project">The path to the Visual Studo project file.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The source language to use (C,C++,Java,AspectJ)</param>
        /// <returns>a SrcMLFile for <paramref name="xmlFileName"/></returns>
        [Obsolete("Consider using GenerateSrcMLFromProject(string,string,Language)")]
        public SrcMLFile GenerateSrcMLFromProject(string project, string xmlFileName, string language)
        {
            Language lang = SrcMLElement.GetLanguageFromString(language);

            GenerateSrcMLFromProject(project, xmlFileName, lang);
            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generate a SrcML file from the given directory.
        /// </summary>
        /// <param name="path">The path to the source directory.</param>
        /// <param name="xmlFileName">The path to write the resulting XML to.</param>
        /// <param name="overrideJava">If true, parse Java files as C++</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        [Obsolete("Consider using GenerateSrcMLFromDirectory(string,string,Dictionary<string,Language>,IEnumerable<string>,Language)")]
        public SrcMLFile GenerateSrcMLFromDirectory(string path, string xmlFileName, bool overrideJava)
        {
            return GenerateSrcMLFromDirectory(path, xmlFileName, "Any", overrideJava, Enumerable.Empty<string>().ToList());
        }

        /// <summary>
        /// Generate a SrcML file from the given directory, with the given languageFilter.
        /// Only source files for the given languageFilter are included in the output.
        /// </summary>
        /// <param name="path">The path to the source directory.</param>
        /// <param name="xmlFileName">The path to write the resulting XML to.</param>
        /// <param name="language">The language to find files for.</param>
        /// <param name="overrideJava">if true, parse Java files as C++</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        [Obsolete("Consider using GenerateSrcMLFromDirectory(string,string,Dictionary<string,Language>,IEnumerable<string>,Language)")]
        public SrcMLFile GenerateSrcMLFromDirectory(string path, string xmlFileName, string language, bool overrideJava)
        {
            return GenerateSrcMLFromDirectory(path, xmlFileName, language, overrideJava, Enumerable.Empty<string>().ToList());
        }

        /// <summary>
        /// Generate a SrcML file from the given directory, with the given languageFilter.
        /// Only source files for the given languageFilter are included in the output.
        /// </summary>
        /// <param name="path">The path to the source directory.</param>
        /// <param name="xmlFileName">The path to write the resulting XML to.</param>
        /// <param name="language">The language to find files for.</param>
        /// <param name="overrideJava">if true, parse Java files as C++</param>
        /// <param name="fileExclusionList">List of files to exclude. This is accomplished via simple string matching; so the entire file path should be used.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        [Obsolete("Consider using GenerateSrcMLFromDirectory(string,string,Dictionary<string,Language>,IEnumerable<string>,Language)")]
        public SrcMLFile GenerateSrcMLFromDirectory(string path, string xmlFileName, string language, bool overrideJava, List<string> fileExclusionList)
        {
            List<string> extensionsToRestore = new List<string>();
            if (overrideJava)
            {
                
                foreach (var kvp in ExtensionMapping)
                {
                    if (kvp.Value == Language.Java)
                    {
                        extensionsToRestore.Add(kvp.Key);
                        ExtensionMapping[kvp.Key] = Language.CPlusPlus;
                    }
                }
            }

            try
            {
                return GenerateSrcMLFromDirectory(path, xmlFileName, fileExclusionList, SrcMLElement.GetLanguageFromString(language));
            }
            finally
            {
                foreach (var ext in extensionsToRestore)
                {
                    ExtensionMapping[ext] = Language.Java;
                }
            }
            
        }

        /// <summary>
        /// Generate a SrcML file from the given directory.
        /// </summary>
        /// <param name="path">The path to the source directory.</param>
        /// <param name="xmlFileName">The path to write the resulting XML to.</param>
        /// <param name="overrideJava">If true, parse Java files as C++</param>
        /// <param name="exclusionListFile">A list of files to exclude from the SrcML document. If null; exclude no files.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        [Obsolete("Consider using GenerateSrcMLFromDirectory(string,string,Dictionary<string,Language>,IEnumerable<string>,Language)")]
        public SrcMLFile GenerateSrcMLFromDirectory(string path, string xmlFileName, bool overrideJava, string exclusionListFile)
        {
            List<string> excludedFiles;
            if (null != exclusionListFile)
                excludedFiles = new List<string>(File.ReadAllLines(exclusionListFile));
            else
                excludedFiles = new List<string>();

            return GenerateSrcMLFromDirectory(path, xmlFileName, "Any", overrideJava, excludedFiles);
        }
#endregion Deprecated APIs
    }
}
