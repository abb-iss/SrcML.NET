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
    /// Represents a do-while loop in a program.
    /// </summary>
    public class DoWhileStatement : ConditionBlockStatement {
        /// <summary> The XML name for DoWhileStatement. </summary>
        public new const string XmlName = "DoWhile";

        /// <summary> Returns the XML name for this program element. </summary>
        public override string GetXmlName() { return DoWhileStatement.XmlName; }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("do ... while({0});", Condition);
        }
    }
}
