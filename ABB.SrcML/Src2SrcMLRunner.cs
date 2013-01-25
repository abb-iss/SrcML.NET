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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;
using System.IO;
using System.Globalization;
using System.Xml.Linq;

namespace ABB.SrcML {
    /// <summary>
    /// Utility class for running src2srcml.exe
    /// </summary>
    public class Src2SrcMLRunner : SrcMLRunner {

        /// <summary>
        /// The src2srcml executable name
        /// </summary>
        public const string Src2SrcMLExecutableName = "src2srcml.exe";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Src2SrcMLRunner"/> class.
        /// </summary>
        public Src2SrcMLRunner()
            : this(SrcMLHelper.GetSrcMLDefaultDirectory()) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Src2SrcMLRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        public Src2SrcMLRunner(string applicationDirectory)
            : this(applicationDirectory, new[] {LIT.ArgumentLabel, OP.ArgumentLabel, TYPE.ArgumentLabel, POS.ArgumentLabel}) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="Src2SrcMLRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        /// <param name="namespaceArguments">The namespace arguments.</param>
        public Src2SrcMLRunner(string applicationDirectory, IEnumerable<string> namespaceArguments)
            : base(applicationDirectory, Src2SrcMLExecutableName, namespaceArguments) {}

        #endregion

        #region Directory Conversion

        /// <summary>
        /// Generate a SrcML document from the given path and place it in the XML file.
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName) {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, Enumerable.Empty<string>(), Language.Any);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files classified as <paramref name="languageFilter"/>.
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <param name="languageFilter">the language to filter on</param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Language languageFilter) {
            return GenerateSrcMLFromDirectory(directoryPath, xmlFileName, Enumerable.Empty<string>(), languageFilter);
        }

        /// <summary>
        /// Generates a SrcML document from the given path and place it in the XML file. The XML document will only contain files not present in <paramref name="filesToExclude"/>
        /// </summary>
        /// <param name="directoryPath">the directory path</param>
        /// <param name="xmlFileName">the path of the xml file</param>
        /// <param name="filesToExclude">A collection of files to exclude from <paramref name="xmlFileName"/></param>
        /// <returns>A SrcMLFile that points at <paramref name="xmlFileName"/></returns>
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude) {
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
        public SrcMLFile GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter) {
            if(!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", directoryPath));

            Collection<string> additionalArguments = new Collection<string>();

            DirectoryInfo dir = new DirectoryInfo(directoryPath);

            var fileObjectsToExclude = from f in filesToExclude
                                       select new FileInfo(f);

            var files = (from filePath in dir.GetFiles("*", SearchOption.AllDirectories)
                         where ExtensionMapping.ContainsKey(filePath.Extension)
                         select filePath).Except(fileObjectsToExclude, new FileInfoComparer());

            IEnumerable<string> reducedFileList;
            if(Language.Any == languageFilter) {
                reducedFileList = from f in files
                                  select f.FullName;
            } else {
                additionalArguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(languageFilter)));
                reducedFileList = from f in files
                                  where languageFilter == ExtensionMapping[f.Extension]
                                  select f.FullName;
            }

            Run(xmlFileName, additionalArguments, new Collection<string>(reducedFileList.ToList()));

            return new SrcMLFile(xmlFileName);
        }

        #endregion

        #region File Conversion

        /// <summary>
        /// Added by JZ on 12/4/2012
        /// Generate both a SrcML XElement and document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XElement representing the source.</returns>
        public XElement GenerateSrcMLAndXElementFromFile(string sourceFileName, string xmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcMLFromFile(sourceFileName, xmlFileName);
            //string srcml = srcMLFile.GetXMLString();
            return srcMLFile.FileUnits.FirstOrDefault();
            //if (srcml != String.Empty)
            //{
            //    return XElement.Parse(srcml, LoadOptions.PreserveWhitespace);
            //}
            //else
            //{
            //    return null;
            //}
        }

        /// <summary>
        /// Added by JZ on 12/3/2012
        /// Generate both a SrcML string and document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XML string representing the source.</returns>
        public string GenerateSrcMLAndStringFromFile(string sourceFileName, string xmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcMLFromFile(sourceFileName, xmlFileName);
            return srcMLFile.GetXMLString();
        }

        /// <summary>
        /// Generate a SrcML document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName) {
            return GenerateSrcMLFromFile(sourceFileName, xmlFileName, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from a single source file with the specified language.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, Language language) {
            var arguments = new Collection<string>();
            if(language > Language.Any) {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));
            }

            arguments.Add(String.Format("\"{0}\"", sourceFileName));

            //Console.WriteLine("sourceFileName = [" + sourceFileName + "]");
            //Console.WriteLine("xmlFileName = [" + xmlFileName + "]");
            Run(xmlFileName, arguments);

            return new SrcMLFile(xmlFileName);
        }

        /// <summary>
        /// Generates a SrcML document from a collection of source files. The languages will be inferred from the file extensions.
        /// </summary>
        /// <param name="sourceFileNames">The source files to generate SrcML from.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName) {
            Run(xmlFileName, new Collection<string>(), new Collection<string>(sourceFileNames.ToList()));
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
            var additionalArguments = new Collection<string>();
            if(language > Language.Any) {
                additionalArguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));
            }

            Run(xmlFileName, additionalArguments, new Collection<string>(sourceFileNames.ToList()));
            return new SrcMLFile(xmlFileName);
        }

        #endregion

        #region String Conversion

        /// <summary>
        /// Generate SrcML from a given string of source code. The source code will be parsed as C++.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcMLFromString(string source) {
            return GenerateSrcMLFromString(source, Language.CPlusPlus);
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

            Collection<string> arguments = new Collection<string>(this.NamespaceArguments);
            arguments.Add("--no-xml-declaration");
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));

            var xml = Run(arguments, source);
            return xml;
        }

        #endregion
    }
}
