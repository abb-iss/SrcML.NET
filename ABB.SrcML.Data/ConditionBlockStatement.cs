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
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    public class ConditionBlockStatement : BlockStatement {
        public const string XmlConditionName = "Condition";

        public ConditionBlockStatement() : base() {}

        public Expression Condition { get; set; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlConditionName == reader.Name) {
                Condition = XmlSerialization.DeserializeExpression(reader);
            }
            base.ReadXmlChild(reader);
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            writer.WriteStartElement(XmlConditionName);
            Condition.WriteXml(writer);
            writer.WriteEndElement();
            base.WriteXmlContents(writer);
        }
    }
}
