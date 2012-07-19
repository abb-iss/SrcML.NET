/******************************************************************************
 * Copyright (c) 2010 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// Class for storing line information for an XNode. LineInfo objects are typically added as annotations to XElements.
    /// </summary>
    public class LineInfo
    {
        int number;
        int position;

        /// <summary>
        /// Create a new LineInfo object with the given line number and position.
        /// </summary>
        /// <param name="lineNumber">the line number</param>
        /// <param name="position">the column number</param>
        public LineInfo(int lineNumber, int position)
        {
            this.number = lineNumber;
            this.position = position;
        }

        /// <summary>
        /// Line number property. The line in the Xml document that the element appears on.
        /// </summary>
        public int LineNumber
        {
            get
            {
                return number;
            }
        }

        /// <summary>
        /// Line position property. The character position in the Xml document that the element appears on.
        /// </summary>
        public int Position
        {
            get
            {
                return position;
            }
        }
    }
}
