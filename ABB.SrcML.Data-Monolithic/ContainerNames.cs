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
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Collections of container names used by some of the mappings
    /// </summary>
    public static class ContainerNames
    {
        /// <summary>
        /// All container names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> All = new ReadOnlyCollection<XName>(new List<XName>() {
            SRC.Block, SRC.Catch, SRC.Class, SRC.Constructor, SRC.Destructor, SRC.Do, SRC.Else, SRC.Enum, SRC.Extern,
            SRC.For, SRC.Function, SRC.If, SRC.Namespace, SRC.Private, SRC.Protected, SRC.Public, SRC.Struct,
            SRC.Switch, SRC.Template, SRC.Then, SRC.Try, SRC.Typedef, SRC.Union, SRC.Unit, SRC.While
        });

        /// <summary>
        /// All type definition names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> TypeDefinitions = new ReadOnlyCollection<XName>(new List<XName>()
        {
            SRC.Class, SRC.Struct, SRC.Typedef
        });

        /// <summary>
        /// All class section names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> ClassSections = new ReadOnlyCollection<XName>(new List<XName>()
        {
            SRC.Private, SRC.Protected, SRC.Public
        });

        /// <summary>
        /// All method-related names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> Methods = new ReadOnlyCollection<XName>(new List<XName>()
        {
            SRC.Constructor, SRC.Destructor, SRC.Function,
            SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration,
            SRC.Call
        });

        /// <summary>
        /// All method names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> MethodDefinitions = new ReadOnlyCollection<XName>(new List<XName>()
        {
            SRC.Constructor, SRC.Destructor, SRC.Function
        });

        /// <summary>
        /// All method declaration names
        /// </summary>
        public static readonly ReadOnlyCollection<XName> MethodDeclarations = new ReadOnlyCollection<XName>(new List<XName>()
        {
            SRC.ConstructorDeclaration, SRC.DestructorDeclaration, SRC.FunctionDeclaration
        });
    }
}
