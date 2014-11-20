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
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace ABB.SrcML {
    /// <summary>
    /// The SRC class contains all of the XNames for SrcML SRC tags.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SRC")]
    public static class SRC {
        /// <summary>
        /// XNamespace for the SrcML SRC XML namespace
        /// </summary>
        public static readonly XNamespace NS = "http://www.sdml.info/srcML/src";

        /// <summary>
        /// XMLNS prefix for the SrcML SRC namespace
        /// </summary>
        public const string Prefix = "src";

        /// <summary>
        /// markup for the unit tag.
        /// 
        /// The unit tag is the basic Container for SrcML. A Unit can represent either a single source file, or a collection of source files (in which case the XML document will have a single root unit, with many child units).
        /// </summary>
        /// TODO document unit attributes.
        public static readonly XName Unit = NS + "unit";

        /// <summary>
        /// XML Markup for the source code block:
        /// 
        /// <code lang="XML">&lt;block&gt;{ ...statements... }&lt;block&gt;</code>
        /// </summary>
        public static readonly XName Block = NS + "block";

        // Comment
        /// <summary>
        /// markup for the comment tag
        /// </summary>
        /// TODO document comment attributes.
        public static readonly XName Comment = NS + "comment";

        // access specifiers
        /// <summary>
        /// markup for the public access specifier.
        /// 
        /// In C++, the following class:break
        /// 
        /// <code lang="C++">
        /// class A
        /// {
        /// public:
        ///     int a;
        /// }
        /// </code>
        /// 
        /// will be marked up as
        /// 
        /// <code lang="XML">
        /// &lt;class&gt;class &lt;name&gt;A&lt;/name&gt;
        /// &lt;block&gt;{&lt;private type="default"&gt;
        /// &lt;/private&gt;&lt;public&gt;public:
        /// &lt;function_decl&gt;&lt;type&gt;&lt;name&gt;int&lt;/name&gt;&lt;/type&gt; &lt;name&gt;a&lt;/name&gt;&lt;parameter_list&gt;()&lt;/parameter_list&gt;;&lt;/function_decl&gt;
        /// &lt;/public&gt;}&lt;/block&gt;&lt;decl/&gt;&lt;/class&gt;
        /// </code>
        /// </summary>
        public static readonly XName Public = NS + "public";

        /// <summary>
        /// markup for the private tag
        /// </summary>
        /// TODO document private attributes.
        public static readonly XName Private = NS + "private";

        /// markup for the public access specifier.
        /// 
        /// In C++, the following class:
        /// 
        /// <code lang="C++">
        /// class A
        /// {
        /// public:
        ///     int a;
        /// }
        /// </code>
        /// 
        /// will be marked up as
        /// 
        /// <code lang="XML">
        ///&lt;class&gt;class &lt;name&gt;A&lt;/name&gt;
        ///&lt;block&gt;{&lt;private type="default"&gt;
        ///&lt;/private&gt;&lt;protected&gt;protected:
        ///	&lt;function_decl&gt;&lt;type&gt;&lt;name&gt;int&lt;/name&gt;&lt;/type&gt; &lt;name&gt;a&lt;/name&gt;&lt;parameter_list&gt;()&lt;/parameter_list&gt;;&lt;/function_decl&gt;
        ///&lt;/protected&gt;}&lt;/block&gt;&lt;decl/&gt;&lt;/class&gt;
        /// </code>
        public static readonly XName Protected = NS + "protected";

        // expressions
        /// <summary>
        /// markup for the expr expression statement tag
        /// </summary>
        public static readonly XName ExpressionStatement = NS + "expr_stmt";

        /// <summary>
        /// markup for the expr tag
        /// </summary>
        public static readonly XName Expression = NS + "expr";

        // declaration
        /// <summary>
        /// markup for the decl tag
        /// </summary>
        public static readonly XName Declaration = NS + "decl";

        /// <summary>
        /// markup for the type tag
        /// </summary>
        public static readonly XName Type = NS + "type";
        /// <summary>
        /// markup for the name tag
        /// </summary>
        public static readonly XName Name = NS + "name";

        /// <summary>
        /// markup for the init tag
        /// </summary>
        public static readonly XName Init = NS + "init";
        /// <summary>
        /// markup for the index tag
        /// </summary>
        public static readonly XName Index = NS + "index";

        /// <summary>
        /// markup for the java package tag
        /// </summary>
        public static readonly XName Package = NS + "package";

        // declaration statement
        /// <summary>
        /// markup for the decl_stmt tag
        /// </summary>
        public static readonly XName DeclarationStatement = NS + "decl_stmt";

        /// <summary>
        /// markup for the typedef tag
        /// </summary>
        public static readonly XName Typedef = NS + "typedef";        // typedef

        /// <summary>
        /// markup for the label tag
        /// </summary>
        public static readonly XName Label = NS + "label";            // label

        /// <summary>
        /// markup for the goto tag
        /// </summary>
        public static readonly XName Goto = NS + "goto";              // goto

        /// <summary>
        /// markup for the asm tag
        /// </summary>
        public static readonly XName Asm = NS + "asm";                // asm

        /// <summary>
        /// markup for the enum tag
        /// </summary>
        public static readonly XName Enum = NS + "enum";              // enum


        // if statement
        /// <summary>
        /// markup for the if tag
        /// </summary>
        public static readonly XName If = NS + "if";
        /// <summary>
        /// markup for the then block tag
        /// </summary>
        public static readonly XName Then = NS + "then";

        /// <summary>
        /// markup for the else tag
        /// </summary>
        public static readonly XName Else = NS + "else";

        // while statement
        /// <summary>
        /// markup for the while tag
        /// </summary>
        public static readonly XName While = NS + "while";

        // do..while statement
        /// <summary>
        /// markup for the do tag
        /// </summary>
        public static readonly XName Do = NS + "do";

        // for statement
        /// <summary>
        /// markup for the for tag
        /// </summary>
        public static readonly XName For = NS + "for";

        /// <summary>
        /// markup for the foreach tag
        /// </summary>
        public static readonly XName Foreach = NS + "foreach";

        /// <summary>
        /// markup for the incr tag
        /// </summary>
        public static readonly XName Increment = NS + "incr";

        /// <summary>
        /// markup for the condition tag
        /// </summary>
        public static readonly XName Condition = NS + "condition";

        /// <summary>
        /// markup for the range tag (used in foreach loops)
        /// </summary>
        public static readonly XName Range = NS + "range";

        /// <summary>
        /// markup for the switch tag
        /// </summary>
        public static readonly XName Switch = NS + "switch";

        /// <summary>
        /// markup for the case tag
        /// </summary>
        public static readonly XName Case = NS + "case";

        /// <summary>
        /// markup for the default tag
        /// </summary>
        public static readonly XName Default = NS + "default";

        /// <summary>
        /// markup for the break tag
        /// </summary>
        public static readonly XName Break = NS + "break";

        /// <summary>
        /// markup for the continue tag
        /// </summary>
        public static readonly XName Continue = NS + "continue";

        // function call
        /// <summary>
        /// markup for the call tag
        /// </summary>
        public static readonly XName Call = NS + "call";

        /// <summary>
        /// markup for the argument_list tag
        /// </summary>
        public static readonly XName ArgumentList = NS + "argument_list";

        /// <summary>
        /// markup for the argument tag
        /// </summary>
        public static readonly XName Argument = NS + "argument";

        // functions
        /// <summary>
        /// markup for the function_prototype tag
        /// </summary>
        public static readonly XName FunctionPrototype = NS + "function_prototype";

        /// <summary>
        /// markup for the function tag
        /// </summary>
        public static readonly XName Function = NS + "function";

        /// <summary>
        /// markup for the  function_decl tag
        /// </summary>
        public static readonly XName FunctionDeclaration = NS + "function_decl";

        /// <summary>
        /// markup for the parameter_list tag
        /// </summary>
        public static readonly XName ParameterList = NS + "parameter_list";

        /// <summary>
        /// markup for the param tag
        /// </summary>
        public static readonly XName Parameter = NS + "param";

        /// <summary>
        /// markup for the specifier tag
        /// </summary>
        public static readonly XName Specifier = NS + "specifier";

        /// <summary>
        /// markup for the return tag
        /// </summary>
        public static readonly XName Return = NS + "return";

        // class elements
        /// <summary>
        /// markup for the class tag
        /// </summary>
        public static readonly XName Class = NS + "class";

        /// <summary>
        /// markup for the class_decl tag
        /// </summary>
        public static readonly XName ClassDeclaration = NS + "class_decl";

        /// <summary>
        /// markup for the struct tag
        /// </summary>
        public static readonly XName Struct = NS + "struct";

        /// <summary>
        /// markup for the struct_decl tag
        /// </summary>
        public static readonly XName StructDeclaration = NS + "struct_decl";

        /// <summary>
        /// markup for the union tag
        /// </summary>
        public static readonly XName Union = NS + "union";

        /// <summary>
        /// markup for the union_decl tag
        /// </summary>
        public static readonly XName UnionDeclaration = NS + "union_decl";

        // methods
        /// <summary>
        /// markup for the Constructor tag
        /// </summary>
        public static readonly XName Constructor = NS + "constructor";

        /// <summary>
        /// markup for the member_list tag
        /// </summary>
        public static readonly XName MemberList = NS + "member_list";

        /// <summary>
        /// markup for the constructor_decl tag
        /// </summary>
        public static readonly XName ConstructorDeclaration = NS + "constructor_decl";

        /// <summary>
        /// markup for the destructor tag
        /// </summary>
        public static readonly XName Destructor = NS + "destructor";

        /// <summary>
        /// markup for the destructor_decl tag
        /// </summary>
        public static readonly XName DestructorDeclaration = NS + "destructor_decl";

        /// <summary>
        /// markup for the super tag
        /// </summary>
        public static readonly XName Super = NS + "super";

        /// <summary>
        /// markup for the extends java tag
        /// </summary>
        public static readonly XName Extends = NS + "extends";

        /// <summary>
        /// markup for the implements java tag
        /// </summary>
        public static readonly XName Implements = NS + "implements";

        /// <summary>
        /// markup for the import java tag
        /// </summary>
        public static readonly XName Import = NS + "import";

        // exception handling elements
        /// <summary>
        /// markup for the try tag
        /// </summary>
        public static readonly XName Try = NS + "try";

        /// <summary>
        /// markup for the throw tag
        /// </summary>
        public static readonly XName Throw = NS + "throw";

        /// <summary>
        /// markup for the catch tag
        /// </summary>
        public static readonly XName Catch = NS + "catch";

        /// <summary>
        /// markup for the finally tag
        /// </summary>
        public static readonly XName Finally = NS + "finally";

        // template elements
        /// <summary>
        /// markup for the template tag
        /// </summary>
        public static readonly XName Template = NS + "template";

        // namespace elements
        /// <summary>
        /// markup for the namespace tag
        /// </summary>
        public static readonly XName Namespace = NS + "namespace";

        /// <summary>
        /// markup for the using tag
        /// </summary>
        public static readonly XName Using = NS + "using";

        /// <summary> markup for the extern tag </summary>
        public static readonly XName Extern = NS + "extern"; // extern

        /// <summary> markup for the macro tag </summary>
        public static readonly XName Macro = NS + "macro"; // macro

        /// <summary> markup for the empty_stmt tag </summary>
        public static readonly XName EmptyStatement = NS + "empty_stmt";

        /// <summary> markup for the sizeof tag </summary>
        public static readonly XName SizeOf = NS + "sizeof";

        /// <summary> markup for the escape tag </summary>
        public static readonly XName Escape = NS + "escape";

        /// <summary> markup for the synchronized tag in Java </summary>
        public static readonly XName Synchronized = NS + "synchronized";

        /// <summary> markup for the attribute tag </summary>
        public static readonly XName Attribute = NS + "attribute";

        /// <summary> markup for the unchecked tag in C# </summary>
        public static readonly XName Unchecked = NS + "unchecked";

        /// <summary> markup for the lock tag in C# </summary>
        public static readonly XName Lock = NS + "lock";
    }
}
