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
    /// Represents a program control structure that contains a condition, e.g. an if statement or while loop.
    /// </summary>
    public abstract class ConditionBlockStatement : BlockStatement {
        private Expression conditionExpression;
        
        /// <summary> XML name for <see cref="Condition"/>. </summary>
        public const string XmlConditionName = "Condition";

        /// <summary> The condition expression controlling the block. </summary>
        public Expression Condition {
            get { return conditionExpression; }
            set {
                conditionExpression = value;
                if(conditionExpression != null) {
                    conditionExpression.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlConditionName == reader.Name) {
                Condition = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Condition) {
                XmlSerialization.WriteElement(writer, Condition, XmlConditionName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(Condition != null) {
                yield return Condition;
            }
        }
    }
}
