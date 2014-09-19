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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a statement declaring an alias in a file.
    /// For example: using File = System.IO.File;
    /// </summary>
    public class AliasStatement : Statement {
        private Expression targetExpression;
        
        /// <summary> The XML name for AliasStatement </summary>
        public new const string XmlName = "Alias";

        /// <summary> XML Name for <see cref="Target" /> </summary>
        public const string XmlTargetName = "Target";

        /// <summary> XML Name for <see cref="AliasName" /> </summary>
        public const string XmlAliasNameName = "AliasName";

        /// <summary> The thing that the alias is pointing to. </summary>
        public Expression Target {
            get { return targetExpression; }
            set {
                targetExpression = value;
                if(targetExpression != null) {
                    targetExpression.ParentStatement = this;
                }
            }
        }

        /// <summary> The new declared name for the target. </summary>
        public string AliasName { get; set; }

        /// <summary>
        /// Instance method for getting <see cref="AliasStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for AliasStatement</returns>
        public override string GetXmlName() { return AliasStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlTargetName == reader.Name) {
                Target = XmlSerialization.ReadChildExpression(reader);
            } else if(XmlAliasNameName == reader.Name) {
                AliasName = reader.ReadElementContentAsString();
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Target) {
                XmlSerialization.WriteElement(writer, Target, XmlTargetName);
            }
            if(!string.IsNullOrEmpty(AliasName)) {
                writer.WriteElementString(XmlAliasNameName, AliasName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(Target != null) {
                yield return Target;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            if(ProgrammingLanguage == Language.Java) {
                return string.Format("import {0}", Target);
            }
            return string.Format("using {0} = {1}", AliasName, Target);
        }
    }
}
