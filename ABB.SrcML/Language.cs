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
using System.Xml.Serialization;

namespace ABB.SrcML
{
    /// <summary>
    /// Enumeration of languages that can be parsed by SrcML.
    /// </summary>
    public enum Language
    {
        /// <summary>Indicates that srcML should choose the language based on file extension.</summary>
        [XmlEnum(Name="Any")]
        Any = 0,
        /// <summary>Indicates that srcML should use the C++ language.</summary>
        [XmlEnum(Name="C++")]
        CPlusPlus,
        /// <summary>Indicates that srcML should use the C language.</summary>
        [XmlEnum(Name="C")]
        C,
        /// <summary>Indicates that srcML should use the Java language.</summary>
        [XmlEnum(Name="Java")]
        Java,
        /// <summary>Indicates that srcML should use the Aspect-J language.</summary>
        [XmlEnum(Name="AspectJ")]
        AspectJ,
        /// <summary>Indicates that srcML should use the C# language.</summary>
        [XmlEnum(Name="C#")]
        CSharp
    }
}
