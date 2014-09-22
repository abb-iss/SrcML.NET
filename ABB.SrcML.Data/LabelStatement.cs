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
    /// <summary>
    /// Represents a label statement in a program.
    /// </summary>
    public class LabelStatement : Statement {
        /// <summary> The XML name for LabelStatement </summary>
        public new const string XmlName = "Label";

        /// <summary> XML Name for <see cref="Name" /> </summary>
        public const string XmlLabelNameName = "LabelName";
        
        /// <summary> The text of the label. </summary>
        public string Name { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="LabelStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for LabelStatement</returns>
        public override string GetXmlName() { return LabelStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlLabelNameName == reader.Name) {
                Name = reader.ReadElementContentAsString();
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(!string.IsNullOrEmpty(Name)) {
                writer.WriteElementString(XmlLabelNameName, Name);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}:", Name);
        }
    }
}
