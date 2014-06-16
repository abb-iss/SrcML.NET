/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    public class ImportStatement : Statement {
        /// <summary>
        /// The XML name for ImportStatement
        /// </summary>
        public new const string XmlName = "Import";

        /// <summary>
        /// XML Name for <see cref="ImportedNamespace" />
        /// </summary>
        public const string XmlImportedNamespaceName = "ImportedNamespace";

        public Expression ImportedNamespace { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="ImportStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ImportStatement</returns>
        public override string GetXmlName() { return ImportStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlImportedNamespaceName == reader.Name) {
                ImportedNamespace = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != ImportedNamespace) {
                XmlSerialization.WriteElement(writer, ImportedNamespace, XmlImportedNamespaceName);
            }
            base.WriteXmlContents(writer);
        }
    }
}
