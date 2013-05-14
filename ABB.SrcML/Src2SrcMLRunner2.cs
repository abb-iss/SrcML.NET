using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public class Src2SrcMLRunner2 {
        private string appDir;

        /// <summary>
        /// The src2srcml executable name
        /// </summary>
        public const string Src2SrcMLExecutableName = "src2srcml.exe";

        public Src2SrcMLRunner2() : this(SrcMLHelper.GetSrcMLDefaultDirectory()) { }

        public Src2SrcMLRunner2(string applicationDirectory) {
            this.ApplicationDirectory = applicationDirectory;
        }

        public string ApplicationDirectory {
            get { return this.appDir; }
            set {
                appDir = value;
                ExecutablePath = Path.Combine(appDir, Src2SrcMLExecutableName);
            }
        }

        public string ExecutablePath { get; private set; }

        public void GenerateSrcMLFromFile(string fileName, string xmlFileName, Language language, Collection<string> namespaceArguments, Dictionary<string,Language> extensionMapping) {
            Collection<string> arguments = GenerateArguments(xmlFileName, language, namespaceArguments, extensionMapping);

            arguments.Add(QuoteFileName(fileName));

            try {
                Run(arguments);
            } catch(SrcMLRuntimeException e) {
                throw new SrcMLException(e.Message, e);
            }
        }

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

        private void Run(Collection<string> arguments) {
            string argumentText = KsuAdapter.MakeArgumentString(arguments);
            KsuAdapter.RunExecutable(ExecutablePath, argumentText);
        }

        private string Run(Collection<string> arguments, string standardInput) {
            string argumentText = KsuAdapter.MakeArgumentString(arguments);
            var output = KsuAdapter.RunExecutable(this.ExecutablePath, argumentText, standardInput);
            return output;
        }

        private static Collection<string> GenerateArguments(string xmlFileName, Language language, Collection<string> namespaceArguments, Dictionary<string, Language> extensionMapping) {
            Collection<string> arguments = new Collection<string>();

            if(language == null) throw new ArgumentNullException("language");
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

        private static string MakeLanguageArgument(Language language) {
            return (language == Language.Any ? String.Empty : String.Format("--language={0}", KsuAdapter.GetLanguage(language)));
        }

        private static string MakeExtensionMapArgument(Dictionary<string, Language> extensionMap) {
            return (extensionMap.Count > 0 ? String.Format("--register-ext {0}", KsuAdapter.ConvertMappingToString(extensionMap)) : String.Empty);
        }

        private static string MakeOutputArgument(string xmlFileName) {
            return String.Format("--output={0}", QuoteFileName(xmlFileName));
        }

        private static string QuoteFileName(string fileName) {
            return String.Format("\"{0}\"", fileName);
        }
    }
}
