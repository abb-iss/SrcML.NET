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

namespace ABB.SrcML.Utilities
{
    internal enum ExecutableReturnValue
    {
        Normal = 0,
        Error = 1,
        ProblemWithInputFile = 2,
        UnknownOption = 3,
        UnknownEncoding = 4,
        InvalidLanguage = 6,
        LanguageOptionSpecifiedButValueMissing = 7,
        FilenameOptionSpecifiedButValueMissing = 8,
        DirectoryOptionSpecifiedButValueMissing = 9,
        VersionOptionSpecifiedButValueMissing = 10,
        TextEncodingOptionSpecifiedButValueMissing = 11,
        XmlEncodingOptionSpecifiedButValueMissing = 12,
        UnitOptionSpecifiedButValueMissing = 13,
        UnitOptionValueIsNotValid = 14,
        InvalidCombinationOfOptions = 15,
        IncompleteOutputDueToTermination = 16
    }
}
