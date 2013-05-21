/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {
    public class SrcMLFileUnitSetup {
        string FileTemplate { get; set; }
        Language SourceLanguage { get; set; }

        public SrcMLFileUnitSetup(Language sourceLanguage) {
            FileTemplate = CreateFileUnitTemplate();
            SourceLanguage = sourceLanguage;
        }

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
            return string.Format("<unit {0} filename=\"{{2}}\" language=\"{{1}}\">{{0}}</unit>", namespaceDecls.ToString());
        }

        public XElement GetFileUnitForXmlSnippet(string xmlSnippet, string fileName) {
            var xml = string.Format(FileTemplate, xmlSnippet, KsuAdapter.GetLanguage(SourceLanguage), fileName);
            var fileUnit = XElement.Parse(xml);
            return fileUnit;
        }
    }
}
