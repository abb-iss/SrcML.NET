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
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;

namespace ABB.SrcML
{
    internal class FileInfoComparer : EqualityComparer<FileInfo>
    {

        public override bool Equals(FileInfo x, FileInfo y)
        {
            if (null == x)
                throw new ArgumentNullException("x");
            if (null == y)
                throw new ArgumentNullException("y");

            return 0 == String.Compare(x.FullName, y.FullName, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode(FileInfo obj)
        {
            if (null == obj)
                throw new ArgumentNullException("obj");

            return obj.FullName.GetHashCode();
        }
    }
}
