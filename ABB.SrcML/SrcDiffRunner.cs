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

using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML
{
    public class SrcDiffRunner : SrcMLRunner {
        /// <summary>
        /// The srcDiff executable name
        /// </summary>
        public const string SrcDiffExecutableName = "srcdiff.exe";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcDiffRunner"/> class.
        /// </summary>
        public SrcDiffRunner()
            : this(SrcToolsHelper.GetSrcMLToolDefaultDirectory("srcdiff")) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcDiffRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        public SrcDiffRunner(string applicationDirectory)
            : this(applicationDirectory, new[] {POS.ArgumentLabel}) {}

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcDiffRunner"/> class.
        /// </summary>
        /// <param name="applicationDirectory">The application directory.</param>
        /// <param name="namespaceArguments">The namespace arguments.</param>
        public SrcDiffRunner(string applicationDirectory, IEnumerable<string> namespaceArguments)
            : base(applicationDirectory, SrcDiffExecutableName, namespaceArguments) {}

        #endregion

        #region File Conversion

        /// <summary>
        /// Generate both a SrcML XElement and document from the difference between <see cref="nameOfOriginalFile"/> and <see cref="modifiedFileName"/>. The language will be inferred from the extension.
        /// </summary>
        /// <param name="nameOfOriginalFile">path to the source file to convert.</param>
        /// <param name="modifiedFileName">The File name to write the resulting XML to.</param>
        /// <param name="outputXmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XElement representing the source.</returns>
        public XElement GenerateSrcDiffAndXElementFromFile(string nameOfOriginalFile, string modifiedFileName, string outputXmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcDiffFromFile(nameOfOriginalFile, modifiedFileName, outputXmlFileName);
            return srcMLFile.FileUnits.FirstOrDefault();
        }

        /// <summary>
        /// Generate both a SrcML string and document from the difference between <see cref="nameOfOriginalFile"/> and <see cref="modifiedFileName"/>. The language will be inferred from the extension.
        /// </summary>
        /// <param name="nameOfOriginalFile">path to the source file to convert.</param>
        /// <param name="nameOfModifiedFile">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XML string representing the source.</returns>
        public string GenerateSrDiffAndStringFromFile(string nameOfOriginalFile, string nameOfModifiedFile, string outputXmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcDiffFromFile(nameOfOriginalFile, nameOfModifiedFile, outputXmlFileName);
            return srcMLFile.GetXMLString();
        }

        /// <summary>
        /// Generate a SrcML document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="nameOfOriginalFile">path to the source file to convert.</param>
        /// <param name="nameOfModifiedFile">The File name to write the resulting XML to.</param>
        /// <param name="outputXmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="outputXmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcDiffFromFile(string nameOfOriginalFile, string nameOfModifiedFile, string outputXmlFileName) {
            return GenerateSrcDiffFromFile(nameOfOriginalFile, nameOfModifiedFile, outputXmlFileName, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from the difference between <see cref="nameOfOriginalFile"/> and <see cref="modifiedFileName"/> with the specified language.
        /// </summary>
        /// <param name="nameOfOriginalFile">The path to the source file to convert.</param>
        /// <param name="nameOfModifiedFile">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcDiffFromFile(string nameOfOriginalFile, string nameOfModifiedFile, string xmlFileName, Language language) {
           // Console.WriteLine("xmlFileName = [" + xmlFileName + "]");
            if (!File.Exists(nameOfOriginalFile))
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", nameOfOriginalFile));
            if (!File.Exists(nameOfModifiedFile))
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", nameOfModifiedFile));

            var arguments = new Collection<string>();
            if(language > Language.Any) {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));
            }

            arguments.Add(String.Format("\"{0}\" \"{1}\"", nameOfOriginalFile, nameOfModifiedFile));

            Run(xmlFileName, arguments);
            return new SrcMLFile(xmlFileName);
        }
        #endregion
    }
}
