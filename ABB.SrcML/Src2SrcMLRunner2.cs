using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    /// <summary>
    /// Simpler version of <see cref="Src2SrcMLRunner"/>. This is a thin wrapper around the src2srcml executable from KSU.
    /// </summary>
    public class Src2SrcMLRunner2 {
        private string appDir;

        /// <summary>
        /// The src2srcml executable name
        /// </summary>
        public const string Src2SrcMLExecutableName = "src2srcml.exe";

        /// <summary>
        /// Constructs a new object with <see cref="ApplicationDirectory"/> set via <see cref="SrcMLHelper.GetSrcMLDefaultDirectory()"/>.
        /// </summary>
        public Src2SrcMLRunner2() : this(SrcMLHelper.GetSrcMLDefaultDirectory()) { }

        /// <summary>
        /// Constructs a new object with <see cref="ApplicationDirectory"/> set to <paramref name="applicationDirectory"/>
        /// </summary>
        /// <param name="applicationDirectory">The directory that contains <see cref="Src2SrcMLExecutableName">src2srcml.exe</see></param>
        public Src2SrcMLRunner2(string applicationDirectory) {
            this.ApplicationDirectory = applicationDirectory;
        }

        /// <summary>
        /// the application directory
        /// </summary>
        public string ApplicationDirectory {
            get { return this.appDir; }
            set {
                appDir = value;
                ExecutablePath = Path.Combine(appDir, Src2SrcMLExecutableName);
            }
        }

        /// <summary>
        /// The full path to src2srcml.exe.
        /// </summary>
        public string ExecutablePath { get; private set; }

        /// <summary>
        /// Generates srcML from a file
        /// </summary>
        /// <param name="fileName">The source file name</param>
        /// <param name="xmlFileName">the output file name</param>
        /// <param name="language">The language to use</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="extensionMapping">an extension mapping</param>
        public void GenerateSrcMLFromFile(string fileName, string xmlFileName, Language language, Collection<string> namespaceArguments, Dictionary<string,Language> extensionMapping) {
            Collection<string> arguments = GenerateArguments(xmlFileName, language, namespaceArguments, extensionMapping);

            arguments.Add(QuoteFileName(fileName));

            try {
                Run(arguments);
            } catch(SrcMLRuntimeException e) {
                throw new SrcMLException(e.Message, e);
            }
        }

        /// <summary>
        /// Generates srcML from a file
        /// </summary>
        /// <param name="fileNames">An enumerable of filenames</param>
        /// <param name="xmlFileName">the output file name</param>
        /// <param name="language">The language to use</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="extensionMapping">an extension mapping</param>
        public void GenerateSrcMLFromFiles(IEnumerable<string> fileNames, string xmlFileName, Language language, Collection<string> namespaceArguments, Dictionary<string, Language> extensionMapping) {
            Collection<string> arguments = GenerateArguments(xmlFileName, language, namespaceArguments, extensionMapping);

            var tempFilePath = Path.GetTempFileName();

            using(StreamWriter writer = new StreamWriter(tempFilePath)) {
                foreach(var sourceFile in fileNames) {
                    writer.WriteLine(sourceFile);
                }
            }

            arguments.Add(String.Format("--files-from={0}", QuoteFileName(tempFilePath)));

            try {
                Run(arguments);
            } catch(SrcMLRuntimeException e) {
                throw new SrcMLException(e.Message, e);
            } finally {
                File.Delete(tempFilePath);
            }
        }

        /// <summary>
        /// Generates srcML from the given string of source code
        /// </summary>
        /// <param name="source">The source code</param>
        /// <param name="language">The language</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="omitXmlDeclaration">If true, the XML header is omitted</param>
        /// <returns>The srcML</returns>
        public string GenerateSrcMLFromString(string source, Language language, Collection<string> namespaceArguments, bool omitXmlDeclaration) {
            var arguments = GenerateArguments(null, language, namespaceArguments, new Dictionary<string, Language>());

            if(omitXmlDeclaration) {
                arguments.Add("--no-xml-declaration");
            }

            try {
                return Run(arguments, source);
            } catch(SrcMLRuntimeException e) {
                throw new SrcMLException(e.Message, e);
            }
        }

        /// <summary>
        /// Runs <see cref="ExecutablePath"/> with the specified arguments
        /// </summary>
        /// <param name="arguments">the arguments</param>
        private void Run(Collection<string> arguments) {
            string argumentText = KsuAdapter.MakeArgumentString(arguments);
            KsuAdapter.RunExecutable(ExecutablePath, argumentText);
        }

        /// <summary>
        /// Runs <see cref="ExecutablePath"/> with the specified arguments. <paramref name="standardInput"/> is passed in to the process's standard input stream
        /// </summary>
        /// <param name="arguments">configuration arguments</param>
        /// <param name="standardInput">contents of standard input</param>
        /// <returns>contents of standard output</returns>
        private string Run(Collection<string> arguments, string standardInput) {
            string argumentText = KsuAdapter.MakeArgumentString(arguments);
            var output = KsuAdapter.RunExecutable(this.ExecutablePath, argumentText, standardInput);
            return output;
        }

        /// <summary>
        /// Generates command line arguments for src2srcml.exe
        /// </summary>
        /// <param name="xmlFileName">the output file name</param>
        /// <param name="language">The programming language</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="extensionMapping">a mapping of file extensions to languages</param>
        /// <returns>A collection of command line arguments</returns>
        private static Collection<string> GenerateArguments(string xmlFileName, Language language, Collection<string> namespaceArguments, Dictionary<string, Language> extensionMapping) {
            Collection<string> arguments = new Collection<string>();

            if(namespaceArguments == null) throw new ArgumentNullException("namespaceArguments");
            if(extensionMapping == null) throw new ArgumentNullException("extensionMapping");

            arguments.Add(MakeLanguageArgument(language));

            foreach(var namespaceArgument in namespaceArguments) {
                arguments.Add(namespaceArgument);
            }

            arguments.Add(MakeExtensionMapArgument(extensionMapping));

            if(!String.IsNullOrEmpty(xmlFileName)) {
                arguments.Add(MakeOutputArgument(xmlFileName));
            }

            return arguments;
        }

        /// <summary>
        /// Converts <paramref name="language"/> to <c>--language=LANGUAGE</c>
        /// </summary>
        /// <param name="language">The language to use</param>
        /// <returns>the language command line parameter</returns>
        private static string MakeLanguageArgument(Language language) {
            return (language == Language.Any ? String.Empty : String.Format("--language={0}", KsuAdapter.GetLanguage(language)));
        }

        /// <summary>
        /// Converts <paramref name="extensionMap"/> to <c>--register-ext EXTENSIONMAP</c>
        /// </summary>
        /// <param name="extensionMap">the extension map to use</param>
        /// <returns>The extension map command line parameter</returns>
        private static string MakeExtensionMapArgument(Dictionary<string, Language> extensionMap) {
            return (extensionMap.Count > 0 ? String.Format("--register-ext {0}", KsuAdapter.ConvertMappingToString(extensionMap)) : String.Empty);
        }

        /// <summary>
        /// Converts <paramref name="xmlFileName"/> to <c>--output="XMLFILENAME"</c>
        /// </summary>
        /// <param name="xmlFileName">the xml file name</param>
        /// <returns>The output command line parameter</returns>
        private static string MakeOutputArgument(string xmlFileName) {
            return String.Format("--output={0}", QuoteFileName(xmlFileName));
        }

        /// <summary>
        /// Surrounds a <paramref name="fileName"/> with quotation marks
        /// </summary>
        /// <param name="fileName">The file name</param>
        /// <returns>The file name, surrounded with double quotes</returns>
        private static string QuoteFileName(string fileName) {
            return String.Format("\"{0}\"", fileName);
        }
    }
}
