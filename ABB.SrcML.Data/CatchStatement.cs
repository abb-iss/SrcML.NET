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
    public class CatchStatement : BlockStatement {
        /// <summary>
        /// The XML name for CatchStatement
        /// </summary>
        public new const string XmlName = "Catch";

        /// <summary>
        /// XML Name for <see cref="Parameter" />
        /// </summary>
        public const string XmlParameterName = "Parameter";

        public VariableDeclaration Parameter { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="CatchStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for CatchStatement</returns>
        public override string GetXmlName() { return CatchStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlParameterName == reader.Name) {
                Parameter = XmlSerialization.ReadChildExpression(reader) as VariableDeclaration;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Parameter) {
                XmlSerialization.WriteElement(writer, Parameter, XmlParameterName);
            }
            base.WriteXmlContents(writer);
        }
    }
}
