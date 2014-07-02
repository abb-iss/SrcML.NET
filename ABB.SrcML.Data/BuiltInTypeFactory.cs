/******************************************************************************
 * Copyright (c) 2013 ABB Group
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

namespace ABB.SrcML.Data {

    /// <summary>
    /// The Built-In type factory creates on-demand instances of built-in types for each language.
    /// It creates and stores one <see cref="TypeDefinition"/> object for each
    /// <see cref="ABB.SrcML.Language"/>/built-in type pair. This factory is primarily used when
    /// comparing <see cref="TypeUse"/> objects for method parameters. A parameter and an argument
    /// should have the same <see cref="TypeDefinition"/> object.
    /// </summary>
    public static class BuiltInTypeFactory {
        private static Dictionary<Tuple<Language, string>, TypeDefinition> builtInTypeMap;

        private static HashSet<string> cppBuiltInParts = new HashSet<string> { "char", "short", "int", "long", "bool", "float", "double", "wchar_t",
                                                                               "signed", "unsigned", "short", "long", "char*"};

        private static HashSet<string> csharpBuiltIns = new HashSet<string> { "byte", "sbyte", "short", "ushort", "int", "uint", "long", "ulong",
                                                                              "float", "double", "decimal", "char", "string", "bool" };

        private static HashSet<string> javaBuiltIns = new HashSet<string> { "byte", "short", "int", "long", "float", "double", "boolean", "char" };

        static BuiltInTypeFactory() {
            builtInTypeMap = new Dictionary<Tuple<Language, string>, TypeDefinition>();
        }

        /// <summary>
        /// Returns the built-in type for the given type use
        /// </summary>
        /// <param name="use">the type use to locate</param>
        /// <returns>A type definition that matches the type use; null if this is not a
        /// built-in</returns>
        public static TypeDefinition GetBuiltIn(TypeUse use) {
            if(!IsBuiltIn(use)) {
                return null;
            }

            var key = new Tuple<Language, string>(use.ProgrammingLanguage, use.Name);
            TypeDefinition builtIn;
            if(!builtInTypeMap.TryGetValue(key, out builtIn)) {
                builtIn = new TypeDefinition() {
                    Accessibility = AccessModifier.None,
                    Kind = TypeKind.BuiltIn,
                    Name = key.Item2,
                    ProgrammingLanguage = key.Item1,
                };
                builtInTypeMap[key] = builtIn;
            }
            return builtIn;
        }

        /// <summary>
        /// Checks if the
        /// <paramref name="use">given type use</paramref> is a built-in type.
        /// </summary>
        /// <param name="use">The type use to test</param>
        /// <returns>true if this is a built-in type; false otherwise</returns>
        public static bool IsBuiltIn(TypeUse use) {
            if(use == null)
                throw new ArgumentNullException("use");

            switch(use.ProgrammingLanguage) {
                case Language.CPlusPlus:
                    return IsCppBuiltIn(use.Name);

                case Language.CSharp:
                    return IsCSharpBuiltIn(use.Name);

                case Language.Java:
                    return IsJavaBuiltIn(use.Name);
            }
            return false;
        }

        private static bool IsCppBuiltIn(string name) {
            var parts = name.Split((char[]) null, StringSplitOptions.RemoveEmptyEntries);
            return parts.All(p => cppBuiltInParts.Contains(p));
        }

        private static bool IsCSharpBuiltIn(string name) {
            return csharpBuiltIns.Contains(name);
        }

        private static bool IsJavaBuiltIn(string name) {
            return javaBuiltIns.Contains(name);
        }
    }
}