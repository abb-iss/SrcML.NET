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
    /// Represents a statement that contains only variable declarations.
    /// This is analogous to the decl_stmt tag in srcML.
    /// </summary>
    public class DeclarationStatement : Statement {
        /// <summary> The XML name for DeclarationStatement. </summary>
        public new const string XmlName = "DeclStmt";

        /// <summary>
        /// Instance method for getting <see cref="ContinueStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ContinueStatement</returns>
        public override string GetXmlName() {
            return DeclarationStatement.XmlName;
        }

        /// <summary>
        /// Returns an enumerable of the variable declarations in this statement.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<VariableDeclaration> GetDeclarations() {
            if(Content != null) {
                return Content.GetDescendantsAndSelf<VariableDeclaration>();
            } else {
                return Enumerable.Empty<VariableDeclaration>();
            }
        }
    }
}
