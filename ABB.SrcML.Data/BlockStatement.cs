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
    public class BlockStatement : Statement {

        public BlockStatement() : base() {}

        /// <summary>
        /// The XML name for BlockStatement
        /// </summary>
        public new const string XmlName = "Block";

        /// <summary>
        /// Instance method for getting <see cref="BlockStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for BlockStatement</returns>
        public override string GetXmlName() { return BlockStatement.XmlName; }

    }
}
