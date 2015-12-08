using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML {
    public interface ISrcMLRunner {
        void GenerateSrcMLFromFile(string fileName, string xmlFileName, Language language, ICollection<UInt32> namespaceArguments, IDictionary<string, Language> extensionMapping);
        void GenerateSrcMLFromFiles(ICollection<string> fileNames, string xmlFileName, Language language, ICollection<UInt32> namespaceArguments, IDictionary<string, Language> extensionMapping);
        string GenerateSrcMLFromString(string source, string unitFilename, Language language, ICollection<UInt32> namespaceArguments, bool omitXmlDeclaration);
        ICollection<string> GenerateSrcMLFromStrings(ICollection<string> sources, ICollection<string> unitFilename, Language language, ICollection<UInt32> namespaceArguments, bool omitXmlDeclaration);
    }
    
}