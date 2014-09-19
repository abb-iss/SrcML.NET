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
    /// Represents a catch statement.
    /// </summary>
    public class CatchStatement : BlockStatement {
        private VariableDeclaration parameter;
        
        /// <summary> The XML name for CatchStatement </summary>
        public new const string XmlName = "Catch";

        /// <summary> XML Name for <see cref="Parameter" /> </summary>
        public const string XmlParameterName = "Parameter";

        /// <summary> The exception that the catch statement will catch. </summary>
        public VariableDeclaration Parameter {
            get { return parameter; }
            set {
                parameter = value;
                if(parameter != null) {
                    parameter.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// Instance method for getting <see cref="CatchStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for CatchStatement</returns>
        public override string GetXmlName() { return CatchStatement.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlParameterName == reader.Name) {
                Parameter = XmlSerialization.ReadChildExpression(reader) as VariableDeclaration;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Parameter) {
                XmlSerialization.WriteElement(writer, Parameter, XmlParameterName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(Parameter != null) {
                yield return Parameter;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("catch({0})", Parameter);
        }
    }
}
