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
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Represents a variable declaration
    /// </summary>
    public class VariableDeclaration : Expression, INamedEntity {
        private TypeUse varType;
        private Expression initExpression;
        private Expression rangeExpression;
        
        /// <summary> The XML name for VariableDeclaration </summary>
        public new const string XmlName = "var";

        /// <summary> XML Name for <see cref="Accessibility" /> </summary>
        public const string XmlAccessibilityName = "Accessibility";

        /// <summary> XML Name for <see cref="Name" /> </summary>
        public const string XmlNameName = "Name";

        /// <summary> XML Name for <see cref="Type" /> </summary>
        public const string XmlTypeName = "Type";

        /// <summary> XML Name for <see cref="Initializer" /> </summary>
        public const string XmlInitializerName = "Initializer";

        /// <summary> XML Name for <see cref="Range"/> </summary>
        public const string XmlRangeName = "Range";

        /// <summary> The access modifier assigned to this type </summary>
        public AccessModifier Accessibility { get; set; }

        /// <summary> The name of the variable </summary>
        public string Name { get; set; }

        /// <summary> Description of the type for this variable </summary>
        public virtual TypeUse VariableType {
            get { return varType; }
            set {
                varType = value;
                if(varType != null) {
                    varType.ParentExpression = this;
                    varType.ParentStatement = this.ParentStatement;
                }
            }
        }

        /// <summary> The expression, if any, used to intialize this variable </summary>
        public Expression Initializer {
            get { return initExpression; }
            set {
                initExpression = value;
                if(initExpression != null) {
                    initExpression.ParentExpression = this;
                    initExpression.ParentStatement = this.ParentStatement;
                }
            }
        }

        /// <summary> The range expression provided for this variable in a foreach loop. </summary>
        public Expression Range {
            get { return rangeExpression; }
            set {
                rangeExpression = value;
                if(rangeExpression != null) {
                    rangeExpression.ParentExpression = this;
                    rangeExpression.ParentStatement = this.ParentStatement;
                }
            }
        }

        /// <summary> The statement containing this expression. </summary>
        public override Statement ParentStatement {
            get { return base.ParentStatement; }
            set {
                base.ParentStatement = value;
                if(VariableType != null) { VariableType.ParentStatement = value; }
                if(Initializer != null) { Initializer.ParentStatement = value; }
            }
        }

        /// <summary>
        /// Returns the child expressions, including the VariableType, Initializer, and Range;
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            if(VariableType != null) {
                yield return VariableType;
            }
            if(Initializer != null) {
                yield return Initializer;
            }
            if(Range != null) {
                yield return Range;
            }
            foreach(var child in base.GetChildren()) {
                yield return child;
            }
        }

        /// <summary>
        /// Returns the locations where this entity appears in the source.
        /// </summary>
        public IEnumerable<SrcMLLocation> GetLocations() {
            yield return Location;
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
            var str = new StringBuilder();
            str.AppendFormat("{0} {1}", VariableType, Name);
            if(Initializer != null) {
                str.AppendFormat(" = {0}", Initializer);
            }
            if(Range != null) {
                if(ProgrammingLanguage == Language.Java) {
                    str.AppendFormat(" : {0}", Range);
                } else {
                    str.AppendFormat(" in {0}", Range);
                }
            }
            return str.ToString();
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlAccessibilityName == reader.Name) {
                Accessibility = AccessModifierExtensions.FromKeywordString(reader.ReadElementContentAsString());
            } else if(XmlNameName == reader.Name) {
                Name = reader.ReadElementContentAsString();
            } else if(XmlTypeName == reader.Name) {
                VariableType = XmlSerialization.ReadChildExpression(reader) as TypeUse;
            } else if(XmlInitializerName == reader.Name) {
                Initializer = XmlSerialization.ReadChildExpression(reader);
            } else if(XmlRangeName == reader.Name) {
                Range = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
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

            if(null != Range) {
                XmlSerialization.WriteElement(writer, Range, XmlRangeName);
            }

            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Determines the type of this variable.
        /// </summary>
        /// <returns>An enumerable of the matching TypeDefinitions for this variable's type.</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            return VariableType != null ? VariableType.ResolveType() : Enumerable.Empty<TypeDefinition>();
        }
    }
}