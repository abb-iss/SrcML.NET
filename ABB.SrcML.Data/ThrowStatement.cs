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
    /// Represents a throw statement in a program.
    /// </summary>
    public class ThrowStatement : Statement {
        /// <summary> The XML name for ThrowStatement </summary>
        public new const string XmlName = "Throw";

        /// <summary>
        /// Instance method for getting <see cref="ThrowStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ThrowStatement</returns>
        public override string GetXmlName() { return ThrowStatement.XmlName; }

        /// <summary>
        /// Returns a string representation of this statement.
        /// </summary>
        public override string ToString() {
            return string.Format("throw {0};", Content);
        }
    }
}
