/******************************************************************************
 * Copyright (c) 2012 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using ABB.SrcML.Test.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Test
{
    class TestConstants
    {
        public static string SrcmlPath { get; private set; }

        static TestConstants() {
            SrcmlPath = Path.Combine(FileUtils.GetSolutionDirectory("SrcML.NET.sln"), "External", "SrcML");
        }
    }
}
