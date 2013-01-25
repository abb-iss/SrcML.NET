using ABB.SrcML.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    static class SrcMLFileUnitSetup {
        public static string CreateFileUnitTemplate() {
            //construct the necessary srcML wrapper unit tags
            XmlNamespaceManager xnm = SrcML.NamespaceManager;
            StringBuilder namespaceDecls = new StringBuilder();
            foreach(string prefix in xnm) {
                if(prefix != string.Empty && !prefix.StartsWith("xml", StringComparison.InvariantCultureIgnoreCase)) {
                    if(prefix.Equals("src", StringComparison.InvariantCultureIgnoreCase)) {
                        namespaceDecls.AppendFormat("xmlns=\"{0}\" ", xnm.LookupNamespace(prefix));
                    } else {
                        namespaceDecls.AppendFormat("xmlns:{0}=\"{1}\" ", prefix, xnm.LookupNamespace(prefix));
                    }
                }
            }
            return string.Format("<unit {0} language=\"{{1}}\">{{0}}</unit>", namespaceDecls.ToString());
        }

        public static XElement GetFileUnitForXmlSnippet(string fileFormat, string xmlSnippet, Language language) {
            var xml = String.Format(fileFormat, xmlSnippet, KsuAdapter.GetLanguage(language));
            var fileUnit = XElement.Parse(xml);
            return fileUnit;
        }
    }
}
