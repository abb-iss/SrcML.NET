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
using System.Xml.Linq;

namespace ABB.SrcML
{
    /// <summary>
    /// SrcML Namespace for C pre-preprocessor directives.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "CPP")]
    public static class CPP
    {
        /// <summary>
        /// XNamespace for the SrcML CPP XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcML/cpp";

        /// <summary>
        /// XMLNS prefix for the SrcML CPP namespace
        /// </summary>
        public const string Prefix = "cpp";

        /// <summary>
        /// Each CPP Directive is surrounded by 
        /// 
        /// #include &lt;windows.h&gt; becomes:
        /// 
        /// &lt;cpp:include&gt;#&lt;cpp:directive&gt;include&lt;/cpp:directive&gt; &lt;cpp:file&gt;&lt;jni.h&gt;&lt;/cpp:file&gt;&lt;/cpp:include&gt;
        /// </summary>
        public static readonly XName Directive = NS + "directive";

        /// <summary>
        /// References to files in CPP directives are surrounded by the File element:
        /// 
        /// #include &lt;windows.h&gt; becomes:
        /// 
        /// &lt;cpp:include&gt;#&lt;cpp:directive&gt;include&lt;/cpp:directive&gt; &lt;cpp:file&gt;&lt;jni.h&gt;&lt;/cpp:file&gt;&lt;/cpp:include&gt;
        /// </summary>
        public static readonly XName File = NS + "file";

        /// <summary>
        /// markup for #include:
        /// 
        /// #include &lt;windows.h&gt; becomes:
        /// 
        /// &lt;cpp:include&gt;#&lt;cpp:directive&gt;include&lt;/cpp:directive&gt; &lt;cpp:file&gt;&lt;jni.h&gt;&lt;/cpp:file&gt;&lt;/cpp:include&gt;
        /// </summary>
        public static readonly XName Include = NS + "include";

        /// <summary>
        /// markup for #define macro
        /// </summary>
        public static readonly XName Define = NS + "define";

        /// <summary>
        /// markup for #undef macro
        /// </summary>
        public static readonly XName Undef = NS + "undef";

        /// <summary>
        /// markup for #if macro
        /// </summary>
        public static readonly XName If = NS + "if";

        /// <summary>
        /// markup for the "then" portion of the #if/#else macro set
        /// </summary>
        public static readonly XName Then = NS + "then";

        /// <summary>
        /// markup for #else tag
        /// </summary>
        public static readonly XName Else = NS + "else";

        /// <summary>
        /// #endif becomes:
        /// 
        /// &lt;cpp:endif&gt;#&lt;cpp:directive&gt;endif&lt;/cpp:directive&gt;&lt;/cpp:endif&gt;
        /// </summary>
        public static readonly XName Endif = NS + "endif";

        /// <summary>
        /// markup for the elif tag
        /// </summary>
        public static readonly XName Elif = NS + "elif";

        /// <summary>
        /// #ifdef __cplusplus becomes:
        /// 
        /// &lt;cpp:ifdef&gt;#&lt;cpp:directive&gt;ifdef&lt;/cpp:directive&gt; &lt;name&gt;__cplusplus&lt;/name&gt;&lt;/cpp:ifdef&gt;
        /// </summary>
        public static readonly XName Ifdef = NS + "ifdef";

        /// <summary>
        /// markup for the #ifndef macro
        /// </summary>
        public static readonly XName Ifndef = NS + "ifndef";

        /// <summary>
        /// markup for the #line macro
        /// </summary>
        public static readonly XName Line = NS + "line";

        /// <summary>
        /// Markup for the #region macro. This is actually used in C#.
        /// </summary>
        public static readonly XName Region = NS + "region";

        /// <summary>
        /// Markup for the #endregion macro. This is actually used in C#.
        /// </summary>
        public static readonly XName EndRegion = NS + "endregion";
    }
}
