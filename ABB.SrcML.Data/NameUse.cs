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
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents the generalized use of a name. This does not distinguish whether the name represents a type, or variable, or what.
    /// </summary>
    public class NameUse : Expression {
        /// <summary>
        /// The XML name for NameUse
        /// </summary>
        public new const string XmlName = "n";

        /// <summary>
        /// XML Name for <see cref="Name" />
        /// </summary>
        public const string XmlNameName = "val";

        /// <summary>
        /// XML Name for <see cref="Prefix" />
        /// </summary>
        public const string XmlPrefixName = "Prefix";

        /// <summary>
        /// The name being used.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prefix of the name. In a fully-qualified name like System.IO.File, the name is File and the prefix is System.IO.
        /// </summary>
        public NamePrefix Prefix { get; set; }

        /// <summary>
        /// The aliases active in the file at the point the name was used.
        /// </summary>
        public Collection<Alias> Aliases { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="NameUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for NameUse</returns>
        public override string GetXmlName() { return NameUse.XmlName; }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlPrefixName == reader.Name) {
                Prefix = XmlSerialization.ReadChildExpression(reader) as NamePrefix;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        protected override void ReadXmlAttributes(XmlReader reader) {
            string attribute = reader.GetAttribute(XmlNameName);
            if(!String.IsNullOrEmpty(attribute)) {
                Name = attribute;
            }
            base.ReadXmlAttributes(reader);
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Prefix) {
                XmlSerialization.WriteElement(writer, Prefix, XmlPrefixName);
            }
            base.WriteXmlContents(writer);
        }

        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString(XmlNameName, Name);
            base.WriteXmlAttributes(writer);
        }
        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            return string.Format("{0}{1}", Prefix, Name);
        }
    }
}
