/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Xml;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a variable declaration
    /// </summary>
    public class VariableDeclaration : Expression {
        /// <summary>
        /// The XML name for VariableDeclaration
        /// </summary>
        public new const string XmlName = "var";

        /// <summary>
        /// XML Name for <see cref="Accessibility" />
        /// </summary>
        public const string XmlAccessibilityName = "Accessibility";

        /// <summary>
        /// XML Name for <see cref="Name" />
        /// </summary>
        public const string XmlNameName = "Name";

        /// <summary>
        /// XML Name for <see cref="Type" />
        /// </summary>
        public const string XmlTypeName = "Type";

        /// <summary>
        /// XML Name for <see cref="Initializer" />
        /// </summary>
        public const string XmlInitializerName = "Initializer";

        /// <summary>
        /// The access modifier assigned to this type
        /// </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary>
        /// The name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the type for this variable
        /// </summary>
        public virtual TypeUse VariableType { get; set; }

        /// <summary>
        /// The expression, if any, used to intialize this variable
        /// </summary>
        public Expression Initializer { get; set; }

        /// <summary>
        /// Returns the child expressions, including the Initializer.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            return Enumerable.Repeat(Initializer, 1).Concat(base.GetChildren());
        }

        /// <summary>
        /// Instance method for getting <see cref="VariableDeclaration.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for VariableDeclaration</returns>
        public override string GetXmlName() { return VariableDeclaration.XmlName; }

        /// <summary>
        /// Returns a string representation of this object.
        /// </summary>
        public override string ToString() {
            //if(Accessibility != AccessModifier.None) {
            //    return string.Format("{0} {1} {2}", Accessibility.ToKeywordString(), VariableType, Name);
            //} else {
            //    return string.Format("{0} {1}", VariableType, Name);
            //}
            return string.Format("{0} {1}", VariableType, Name);
        }

        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlAccessibilityName == reader.Name) {
                Accessibility = AccessModifierExtensions.FromKeywordString(reader.ReadContentAsString());
            } else if(XmlNameName == reader.Name) {
                Name = reader.ReadContentAsString();
            } else if(XmlTypeName == reader.Name) {
                VariableType = XmlSerialization.ReadChildExpression(reader) as TypeUse;
            } else if(XmlInitializerName == reader.Name) {
                Initializer = XmlSerialization.ReadChildExpression(reader);
            }
            base.ReadXmlChild(reader);
        }

        protected override void WriteXmlContents(XmlWriter writer) {
            if(AccessModifier.None != Accessibility) {
                writer.WriteElementString(XmlAccessibilityName, Accessibility.ToKeywordString());
            }
            
            if(null != VariableType) {
                XmlSerialization.WriteElement(writer, VariableType, XmlTypeName);
            }
            
            writer.WriteElementString(XmlNameName, Name);

            if(null != Initializer) {
                XmlSerialization.WriteElement(writer, Initializer, XmlInitializerName);
            }
            base.WriteXmlContents(writer);
        }
        
    }
}