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
    /// <summary>
    /// Represents a case statement within a switch.
    /// </summary>
    public class CaseStatement : ConditionBlockStatement {
        /// <summary> The XML name for CaseStatement </summary>
        public new const string XmlName = "Case";
        
        /// <summary> XML Name for <see cref="IsDefault" /> </summary>
        public const string XmlIsDefaultName = "IsDefault";

        /// <summary> Indicates whether this case is the default for the switch. </summary>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="CaseStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for CaseStatement</returns>
        public override string GetXmlName() { return CaseStatement.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            base.ReadXmlChild(reader);
        }

        protected override void WriteXmlAttributes(XmlWriter writer) {
            if(IsDefault) {
                writer.WriteAttributeString(XmlIsDefaultName, XmlConvert.ToString(IsDefault));
            }
            base.WriteXmlAttributes(writer);
        }

        protected override void ReadXmlAttributes(XmlReader reader) {
            var isDefaultAttribute = reader.GetAttribute(XmlIsDefaultName);
            if(null != isDefaultAttribute) {
                IsDefault = XmlConvert.ToBoolean(isDefaultAttribute);
            }
            base.ReadXmlAttributes(reader);
        }
    }
}
