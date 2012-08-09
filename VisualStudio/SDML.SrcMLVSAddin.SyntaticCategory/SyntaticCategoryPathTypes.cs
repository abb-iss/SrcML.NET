/******************************************************************************
 * Copyright (c) 2011 Brian Bartman
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Brian Bartman (SDML) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SDML.SrcMLVSAddin.SyntaticCategory
{
    /// <summary>
    /// An enumeration denoting which type of syntatic category to 
    /// look for while handling each syntatic occurance of the
    /// search pattern.
    /// </summary>
    public enum SyntaticCategoryPathTypes
    {
        /// <summary>
        /// This is only valid in the event that the out. If the search
        /// pattern is looking for an expression or expr node and the search expression
        /// isn't searching for a statement/s.
        /// </summary>
        OuterStatmentCategory,

        /// <summary>
        /// Causes the syntatic category to be generated based on
        /// the searching of parent nodes until either a block or a
        /// translation unit.
        /// </summary>
        OuterBlockCategory,

        /// <summary>
        /// This looks for the OUTER most block being one which is nested inside of
        /// a translation unit.
        /// </summary>
        OuterMostBlockCategory
    }
}
