using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using KsuAdapter = ABB.SrcML.Utilities.KsuAdapter;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The SourceFileParser class generates data from srcML files
    /// </summary>
    public class CodeParser {
        public static IEnumerable<TypeDefinition> CreateTypeDefinitions(XElement fileUnit) {
            Language language = SrcMLElement.GetLanguageForUnit(fileUnit);

            HashSet<XName> typeElementHash = new HashSet<XName>(new XName[] { SRC.Class, SRC.Struct, SRC.Typedef, SRC.Union });
            
            var typeElements = from typeElement in fileUnit.Descendants()
                               where typeElementHash.Contains(typeElement.Name)
                               select typeElement;

            foreach(var typeElement in typeElements) {
                var typeKind = typeElement.Name;
                var typeNameElement = typeElement.Element(SRC.Name);
            }
            throw new NotImplementedException();
        }

        public static TypeUse CreateTypeUse(XElement declarationStatementElement) {
            throw new NotImplementedException();
        }

        public static void GetIncludedNamespaces(XElement fileUnit) {
            
        }
    }
}
