﻿/******************************************************************************
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
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a use of a literal in a program.
    /// For example, in "int a = 42;" 42 is a literal use.
    /// </summary>
    //[Serializable]
    public class LiteralUse : Expression {

        /// <summary>
        /// The XML name for LiteralUse
        /// </summary>
        public new const string XmlName = "lu";

        /// <summary>
        /// XML Name for <see cref="Kind" />
        /// </summary>
        public const string XmlKindName = "Kind";

        /// <summary>
        /// XML Name for <see cref="Value" />
        /// </summary>
        public const string XmlValueName = "Value";

        /// <summary>The text of the literal.</summary>
        public string Value { get; set; }

        /// <summary>The kind of literal.</summary>
        public LiteralKind Kind { get; set; }

        /// <summary>
        /// Gets the literal kind from the
        /// <paramref name="literalElement"/></summary>
        /// <param name="literalElement">The literal element</param>
        /// <returns>The kind of element this is</returns>
        public static LiteralKind GetLiteralKind(XElement literalElement) {
            if(literalElement == null)
                throw new ArgumentNullException("literalElement");
            if(literalElement.Name != LIT.Literal)
                throw new ArgumentException("should be of type LIT.Literal", "literalElement");

            var typeAttribute = literalElement.Attribute("type");
            if(null == typeAttribute)
                throw new ArgumentException("should contain a \"type\" attribute", "literalElement");

            var kind = typeAttribute.Value;
            if(kind == "boolean")
                return LiteralKind.Boolean;
            else if(kind == "char")
                return LiteralKind.Character;
            else if(kind == "number")
                return LiteralKind.Number;
            else if(kind == "string")
                return LiteralKind.String;
            throw new SrcMLException(String.Format("\"{0}\" is not a valid literal kind", kind));
        }

        /// <summary>
        /// Instance method for getting <see cref="LiteralUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for LiteralUse</returns>
        public override string GetXmlName() { return LiteralUse.XmlName; }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            string attribute = reader.GetAttribute(XmlKindName);
            if(!String.IsNullOrEmpty(attribute)) {
                Kind = LiteralKindExtensions.FromKeyword(attribute);
            }
            attribute = reader.GetAttribute(XmlValueName);
            if(!String.IsNullOrEmpty(attribute)) {
                Value = attribute;
            }

            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Writes XML attributes from this object to the XML writer
        /// </summary>
        /// <param name="writer">The XML writer</param>
        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString(XmlKindName, Kind.ToKeyword());
            writer.WriteAttributeString(XmlValueName, Value);
            base.WriteXmlAttributes(writer);
        }
        /// <summary> Returns the text value of this literal. </summary>
        public override string ToString() {
            return Value;
        }
    }
}