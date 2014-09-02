/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents the use of an operator in an expression.
    /// </summary>
    public class OperatorUse : Expression {

        /// <summary> The XML name for OperatorUse </summary>
        public new const string XmlName = "op";

        /// <summary> XML Name for <see cref="Text" /> </summary>
        public const string XmlTextName = "text";
        
        /// <summary> The text of the operator. </summary>
        public string Text { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="OperatorUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for OperatorUse</returns>
        public override string GetXmlName() { return OperatorUse.XmlName; }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            var textAttribute = reader.GetAttribute(XmlTextName);
            if(!String.IsNullOrEmpty(textAttribute)) {
                Text = textAttribute;
            }
            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Writes XML attributes from this object to the XML writer
        /// </summary>
        /// <param name="writer">The XML writer</param>
        protected override void WriteXmlAttributes(XmlWriter writer) {
            if(!string.IsNullOrEmpty(Text)) {
                writer.WriteAttributeString(XmlTextName, Text);
            }
            base.WriteXmlAttributes(writer);
        }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            return Text;
        }
    }
}
