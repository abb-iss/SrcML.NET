/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Text;
using System.IO;

namespace ABB.SrcML.VisualStudio.PreviewAddIn
{
    public class DirectoryInfoComparer : EqualityComparer<DirectoryInfo>
    {
        public override bool Equals(DirectoryInfo x, DirectoryInfo y)
        {
            return (x.FullName.Equals(y.FullName, StringComparison.OrdinalIgnoreCase));
        }

        public override int GetHashCode(DirectoryInfo obj)
        {
            return obj.FullName.GetHashCode();
        }
    }
}
