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

namespace ABB.SrcML.Data
{
    public class ForStatement : ConditionBlockStatement {
        /// <summary>
        /// XML Name for <see cref="Initializer" />
        /// </summary>
        public const string XmlInitializerName = "Initializer";

        /// <summary>
        /// XML Name for <see cref="Incrementer" />
        /// </summary>
        public const string XmlIncrementerName = "Incrementer";

        /// <summary>
        /// The XML name for ForStatement
        /// </summary>
        public new const string XmlName = "For";

        public Expression Initializer { get; set; }
        public Expression Incrementer { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="ForStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ForStatement</returns>
        public override string GetXmlName() { return ForStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlInitializerName == reader.Name) {
                Initializer = XmlSerialization.ReadChildExpression(reader);
            } else if(XmlIncrementerName == reader.Name) {
                Incrementer = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Initializer) {
                XmlSerialization.WriteElement(writer, Initializer, XmlInitializerName);
            }
            if(null != Incrementer) {
                XmlSerialization.WriteElement(writer, Incrementer, XmlIncrementerName);
            }
            
            base.WriteXmlContents(writer);
        }
    }
}
