/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    public class LabelStatement : Statement {
        /// <summary>
        /// The XML name for LabelStatement
        /// </summary>
        public new const string XmlName = "Label";

        /// <summary>
        /// XML Name for <see cref="LabelName" />
        /// </summary>
        public const string XmlLabelNameName = "LabelName";
        public string Name { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="LabelStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for LabelStatement</returns>
        public override string GetXmlName() { return LabelStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlLabelNameName == reader.Name) {
                Name = reader.ReadElementContentAsString();
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Name) {
                writer.WriteElementString(XmlLabelNameName, Name);
            }
            base.WriteXmlContents(writer);
        }
    }
}
