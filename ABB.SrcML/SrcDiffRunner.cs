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
    public class SrcDiffRunner : SrcMLRunner
    {
        /// <summary>
        /// The src2srcml executable name
        /// </summary>
        public const string SrcDiffExecutableName = "srcdiff.exe";

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SrcDiffRunner"/> class.
        /// </summary>
        public SrcDiffRunner()
            : this(SrcMLHelper.GetSrcMLDefaultDirectory()) {}

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
        /// Added by JZ on 12/4/2012
        /// Generate both a SrcML XElement and document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="modifiedFileName">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XElement representing the source.</returns>
        public XElement GenerateSrcDiffAndXElementFromFile(string sourceFileName, string modifiedFileName, string xmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcDiffFromFile(sourceFileName, modifiedFileName, xmlFileName);
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
        /// <param name="modifiedXmlFile">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>An XML string representing the source.</returns>
        public string GenerateSrDiffAndStringFromFile(string sourceFileName, string modifiedXmlFile, string xmlFileName) {
            SrcMLFile srcMLFile = GenerateSrcDiffFromFile(sourceFileName, modifiedXmlFile, xmlFileName);
            return srcMLFile.GetXMLString();
        }

        /// <summary>
        /// Generate a SrcML document from a single source file. The language will be inferred from the extension.
        /// </summary>
        /// <param name="sourceFileName">path to the source file to convert.</param>
        /// <param name="modifiedXmlFile">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The File name to write the resulting XML to.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcDiffFromFile(string sourceFileName, string modifiedXmlFile, string xmlFileName) {
            return GenerateSrcDiffFromFile(sourceFileName, modifiedXmlFile, xmlFileName, Language.Any);
        }

        /// <summary>
        /// Generate a SrcML document from a single source file with the specified language.
        /// </summary>
        /// <param name="sourceFileName">The path to the source file to convert.</param>
        /// <param name="modifiedXmlFile">The File name to write the resulting XML to.</param>
        /// <param name="xmlFileName">The file name to write the resulting XML to.</param>
        /// <param name="language">The language to parse the source file as.</param>
        /// <returns>A SrcMLFile for <paramref name="xmlFileName"/>.</returns>
        public SrcMLFile GenerateSrcDiffFromFile(string sourceFileName, string modifiedXmlFile, string xmlFileName, Language language) {
           // Console.WriteLine("xmlFileName = [" + xmlFileName + "]");
            if (!File.Exists(sourceFileName))
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", sourceFileName));
            if (!File.Exists(modifiedXmlFile))
                throw new FileNotFoundException(String.Format(CultureInfo.CurrentCulture, "{0} does not exist.", modifiedXmlFile));

            var arguments = new Collection<string>();
            if(language > Language.Any) {
                arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));
            }

            arguments.Add(String.Format("\"{0}\" \"{1}\"", sourceFileName, modifiedXmlFile));
            //Console.Write(String.Format("\"{0} {1}\"", sourceFileName, modifiedXmlFile));

            //Console.WriteLine("sourceFileName = [" + sourceFileName + "]");
            //Console.WriteLine("xmlFileName = [" + xmlFileName + "]");
            Run(xmlFileName, arguments);

            Console.WriteLine("CONTENTS: {0}", xmlFileName);
            return new SrcMLFile(xmlFileName);
        }
        #endregion

        #region String Conversion

        /// <summary>
        /// Generate SrcML from a given string of source code. The source code will be parsed as C++.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcDiffFromString(string source) {
            return GenerateSrcDiffFromString(source, Language.CPlusPlus);
        }

        /// <summary>
        /// Generate SrcML from a given string of source code.
        /// </summary>
        /// <param name="source">A string containing the source code to parse.</param>
        /// <param name="language">The language to parse the string as. Language.Any is not valid.</param>
        /// <returns>XML representing the source.</returns>
        public string GenerateSrcDiffFromString(string source, Language language) {
            if(language == Language.Any) {
                throw new SrcMLException("Any is not a valid language. Pick an actual language in the enumeration");
            }

            Collection<string> arguments = new Collection<string>(this.NamespaceArguments);
            arguments.Add("--no-xml-decl");
            arguments.Add(String.Format(CultureInfo.InvariantCulture, "--language={0}", KsuAdapter.GetLanguage(language)));

            var xml = Run(arguments, source);
            return xml;
        }

        #endregion
    }
}
