using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public interface ISrcMLGenerator {
        Dictionary<string, Language> ExtensionMapping { get; }

        void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName);

        void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude);

        void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, Language languageFilter);

        void GenerateSrcMLFromDirectory(string directoryPath, string xmlFileName, IEnumerable<string> filesToExclude, Language languageFilter);

        void GenerateSrcMLFromFile(string sourceFileName, string xmlFileName);

        void GenerateSrcMLFromFile(string sourceFileName, string xmlFileName, Language language);

        void GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName);

        void GenerateSrcMLFromFiles(IEnumerable<string> sourceFileNames, string xmlFileName, Language language);

        string GenerateSrcMLFromString(string source, Language language);
    }
}
