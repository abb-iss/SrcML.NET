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

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a while-loop in a program.
    /// </summary>
    public class WhileStatement : ConditionBlockStatement {
        /// <summary> The XML name for WhileStatement </summary>
        public new const string XmlName = "While";

        /// <summary>
        /// Instance method for getting <see cref="WhileStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for WhileStatement</returns>
        public override string GetXmlName() { return WhileStatement.XmlName; }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("while({0})", Condition);
        }
    }
}
