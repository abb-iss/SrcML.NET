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
    /// Represents an extern statement in C/C++ that specifies a linkage type.
    /// Note that this is expected to be something like <code>extern "C" { #include&lt;stdio.h&gt; }</code>.
    /// 
    /// Declarations that use extern as a storage specifier, such as <code>extern int myGlobalVar;</code>, will not be parsed as ExternStatements.
    /// </summary>
    public class ExternStatement : Statement {
        /// <summary> The XML name for ExternStatement. </summary>
        public new const string XmlName = "Extern";

        /// <summary> XML Name for <see cref="LinkageType" /> </summary>
        public const string XmlLinkageTypeName = "LinkageType";

        /// <summary>
        /// The specified linkage type.
        /// For example, in <code>extern "C" { #include&lt;stdio.h&gt; }</code> the linkage type is C.
        /// </summary>
        public string LinkageType { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="ExternStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ExternStatement</returns>
        public override string GetXmlName() { return ExternStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlLinkageTypeName == reader.Name) {
                LinkageType = reader.ReadElementContentAsString();
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(!string.IsNullOrEmpty(LinkageType)) {
                writer.WriteElementString(XmlLinkageTypeName, LinkageType);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format(@"extern ""{0}""", LinkageType);
        }
    }
}
