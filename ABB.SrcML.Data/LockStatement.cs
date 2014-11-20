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
    /// Represents a lock statement in C#.
    /// These are of the form: 
    /// <code> lock(myObj) { ... } </code>
    /// </summary>
    public class LockStatement : BlockStatement {
        private Expression lockExpression;

        /// <summary> The XML name for LockStatement </summary>
        public new const string XmlName = "Lock";

        /// <summary> XML Name for <see cref="LockExpression" /> </summary>
        public const string XmlLockExpressionName = "LockExpression";

        /// <summary> The expression specifying the object being locked. </summary>
        public Expression LockExpression {
            get { return lockExpression; }
            set {
                lockExpression = value;
                if(lockExpression != null) {
                    lockExpression.ParentStatement = this;
                }
            }
        }

        /// <summary>
        /// Instance method for getting <see cref="LockStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for LockStatement</returns>
        public override string GetXmlName() {
            return LockStatement.XmlName;
        }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlLockExpressionName == reader.Name) {
                LockExpression = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != LockExpression) {
                XmlSerialization.WriteElement(writer, LockExpression, XmlLockExpressionName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Returns all the expressions within this statement.
        /// </summary>
        public override IEnumerable<Expression> GetExpressions() {
            if(LockExpression != null) {
                yield return LockExpression;
            }
        }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("lock({0})", LockExpression);
        }
    }
}
