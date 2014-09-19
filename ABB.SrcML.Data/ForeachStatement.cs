/******************************************************************************
 * Copyright (c) 2014 ABB Group
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

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a foreach statement in a program.
    /// </summary>
    public class ForeachStatement : ConditionBlockStatement {
        /// <summary> The XML name for ForeachStatement </summary>
        public new const string XmlName = "Foreach";

        /// <summary>
        /// Instance method for getting <see cref="ForeachStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ForeachStatement</returns>
        public override string GetXmlName() { return ForeachStatement.XmlName; }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            if(ProgrammingLanguage == Language.Java) {
                return string.Format("for({0})", Condition);
            }
            return string.Format("foreach({0})", Condition);
        }
    }
}
