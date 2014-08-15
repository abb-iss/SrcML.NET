/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System.Collections.Generic;
namespace ABB.SrcML.Data {

    /// <summary>
    /// Enumerates the kinds of types encountered in the supported programming languages.
    /// </summary>
    public enum TypeKind {

        /// <summary>
        /// Built-In type
        /// </summary>
        BuiltIn = 0,

        /// <summary>
        /// Class type
        /// </summary>
        Class,

        /// <summary>
        /// Struct type
        /// </summary>
        Struct,

        /// <summary>
        /// Union type
        /// </summary>
        Union,

        /// <summary>
        /// Interface type
        /// </summary>
        Interface,

        /// <summary>
        /// Enumeration type
        /// </summary>
        Enumeration
    }

    /// <summary>
    /// Contains extension methods for the TypeKind enum.
    /// </summary>
    public static class TypeKindExtensions {
        /// <summary> Returns a keyword string for this TypeKind. </summary>
        public static string ToKeyword(this TypeKind tk) {
            switch(tk) {
                case TypeKind.BuiltIn:
                    return "built-in";
                case TypeKind.Class:
                    return "class";
                case TypeKind.Struct:
                    return "struct";
                case TypeKind.Union:
                    return "union";
                case TypeKind.Interface:
                    return "interface";
                case TypeKind.Enumeration:
                    return "enumeration";
                default:
                    return "built-in";
            }
        }

        /// <summary> Returns a LiteralKind for the given keyword string. </summary>
        public static TypeKind FromKeyword(string keyword) {
            var mapping = new Dictionary<string, TypeKind>() {
                {"built-in", TypeKind.BuiltIn},
                {"class", TypeKind.Class},
                {"struct", TypeKind.Struct},
                {"union", TypeKind.Union},
                {"interface", TypeKind.Interface},
                {"enumeration", TypeKind.Enumeration},
            };
            TypeKind output;
            if(!string.IsNullOrEmpty(keyword) && mapping.TryGetValue(keyword, out output)) {
                return output;
            }
            return TypeKind.BuiltIn;
        }
    }
}