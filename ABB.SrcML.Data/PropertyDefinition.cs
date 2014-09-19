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

using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a property in C#.
    /// </summary>
    public class PropertyDefinition : NamedScope {
        private MethodDefinition getter;
        private MethodDefinition setter;
        private TypeUse returnType;

        /// <summary> The XML name for PropertyDefinition </summary>
        public new const string XmlName = "Property";

        /// <summary> XML Name for <see cref="Getter" /> </summary>
        public const string XmlGetterName = "Getter";

        /// <summary> XML Name for <see cref="Setter" /> </summary>
        public const string XmlSetterName = "Setter";

        /// <summary> XML Name for <see cref="ReturnType" /> </summary>
        public const string XmlReturnTypeName = "ReturnType";

        /// <summary> Creates a new default PropertyDefinition. </summary>
        public PropertyDefinition() : base() {
            getter = null;
            setter = null;
        }

        /// <summary> The type of the property. </summary>
        public TypeUse ReturnType {
            get { return returnType; }
            set {
                returnType = value;
                if(returnType != null) {
                    returnType.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// The getter method for this property, if any.
        /// </summary>
        public MethodDefinition Getter {
            get {
                if(getter == null) {
                    getter = SearchForGetter();
                }
                return getter;
            }
        }

        /// <summary>
        /// The setter method for this property, if any.
        /// </summary>
        public MethodDefinition Setter {
            get {
                if(setter == null) {
                    setter = SearchForSetter();
                }
                return setter;
            }
        }

        /// <summary>
        /// Adds the given Statement to the ChildStatements collection.
        /// Also assigns it to Getter or Setter, as appropriate.
        /// </summary>
        /// <param name="child">The Statement to add.</param>
        public override void AddChildStatement(Statement child) {
            base.AddChildStatement(child);
            if(null != child) {
                var method = child as MethodDefinition;
                if(method != null) {
                    if(method.Name == "get") {
                        this.getter = method;
                    } else if(method.Name == "set") {
                        this.setter = method;
                    }
                }
            }
        }

        /// <summary>
        /// Instance method for getting <see cref="PropertyDefinition.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for PropertyDefinition</returns>
        public override string GetXmlName() { return PropertyDefinition.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlGetterName == reader.Name) {
                getter = XmlSerialization.ReadChildStatement(reader) as MethodDefinition;
            } else if(XmlSetterName == reader.Name) {
                setter = XmlSerialization.ReadChildStatement(reader) as MethodDefinition;
            } else if(XmlReturnTypeName == reader.Name) {
                ReturnType = XmlSerialization.ReadChildExpression(reader) as TypeUse;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            base.WriteXmlContents(writer);
            if(null != Getter) {
                XmlSerialization.WriteElement(writer, Getter, XmlGetterName);
            }
            if(null != Setter) {
                XmlSerialization.WriteElement(writer, Setter, XmlSetterName);
            }
            if(null != ReturnType) {
                XmlSerialization.WriteElement(writer, ReturnType, XmlReturnTypeName);
            }
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(ReturnType != null) {
                yield return ReturnType;
            }
            if(Prefix != null) {
                yield return Prefix;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            if(Accessibility == AccessModifier.None) {
                return string.Format("{0} {1}", ReturnType, Name);
            } else {
                return string.Format("{0} {1} {2}", Accessibility.ToKeywordString(), ReturnType, Name);
            }
        }

        #region Private Methods
        private MethodDefinition SearchForGetter() {
            return ChildStatements.OfType<MethodDefinition>().FirstOrDefault(m => m.Name == "get");
        }

        private MethodDefinition SearchForSetter() {
            return ChildStatements.OfType<MethodDefinition>().FirstOrDefault(m => m.Name == "set");
        }
        #endregion
    }
}
