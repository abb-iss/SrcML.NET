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
using ABB.SrcML;

namespace ABB.SrcML.Tools.Converter
{
    class ExtensionLanguagePair
    {
        public ExtensionLanguagePair(string extension, Language language)
        {
            Extension = extension;
            Language = language;
        }
        public string Extension { get; set; }
        public Language Language { get; set; }
    }
}
