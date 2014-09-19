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
    /// Represents a continue statement.
    /// </summary>
    public class ContinueStatement : Statement {
        /// <summary> The XML name for ContinueStatement </summary>
        public new const string XmlName = "Continue";

        /// <summary>
        /// Instance method for getting <see cref="ContinueStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ContinueStatement</returns>
        public override string GetXmlName() { return ContinueStatement.XmlName; }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return "continue;";
        }
    }
}
