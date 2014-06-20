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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    public class AliasStatement : Statement {
        /// <summary>
        /// The XML name for AliasStatement
        /// </summary>
        public new const string XmlName = "Alias";

        /// <summary>
        /// XML Name for <see cref="Target" />
        /// </summary>
        public const string XmlTargetName = "Target";

        /// <summary>
        /// XML Name for <see cref="AliasName" />
        /// </summary>
        public const string XmlAliasNameName = "AliasName";

        public Expression Target { get; set; }
        public string AliasName { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="AliasStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for AliasStatement</returns>
        public override string GetXmlName() { return AliasStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlTargetName == reader.Name) {
                Target = XmlSerialization.ReadChildExpression(reader);
            } else if(XmlAliasNameName == reader.Name) {
                AliasName = reader.ReadElementContentAsString();
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Target) {
                XmlSerialization.WriteElement(writer, Target, XmlTargetName);
            }
            if(!string.IsNullOrEmpty(AliasName)) {
                writer.WriteElementString(XmlAliasNameName, AliasName);
            }
            base.WriteXml(writer);
        }
    }
}
