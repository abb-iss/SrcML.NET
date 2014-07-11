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

using System;
using System.Collections.Generic;
namespace ABB.SrcML.Data {

    /// <summary>
    /// Enumerates the types of protection encountered in the supported programming languages
    /// </summary>
    public enum AccessModifier {

        /// <summary>
        /// None indicates that no access modifier was provided
        /// </summary>
        None = 0,

        /// <summary>
        /// Private
        /// </summary>
        Private,

        /// <summary>
        /// Internal
        /// </summary>
        Internal,

        /// <summary>
        /// Protected
        /// </summary>
        Protected,

        /// <summary>
        /// Protected Internal, used in C#
        /// </summary>
        ProtectedInternal,

        /// <summary>
        /// Public
        /// </summary>
        Public,
    }

    /// <summary>
    /// Contains extension methods for the AccessModifier enum.
    /// </summary>
    public static class AccessModifierExtensions {

        /// <summary>
        /// Converts the enum value to its programming language keyword equivalent.
        /// </summary>
        public static string ToKeywordString(this AccessModifier am) {
            switch(am) {
                case AccessModifier.None:
                    return string.Empty;

                case AccessModifier.Public:
                    return "public";

                case AccessModifier.ProtectedInternal:
                    return "protected internal";

                case AccessModifier.Protected:
                    return "protected";

                case AccessModifier.Internal:
                    return "internal";

                case AccessModifier.Private:
                    return "private";

                default:
                    return am.ToString();
            }
        }

        /// <summary>
        /// Converts a keyword string to its enum value.
        /// </summary>
        public static AccessModifier FromKeywordString(string am) {
            Dictionary<string, AccessModifier> mapping = new Dictionary<string,AccessModifier>() {
                { "public", AccessModifier.Public },
                { "protected internal", AccessModifier.ProtectedInternal },
                { "protected", AccessModifier.Protected },
                { "internal", AccessModifier.Internal },
                { "private", AccessModifier.Private },
            };
            AccessModifier output;
            if(!String.IsNullOrEmpty(am) && mapping.TryGetValue(am, out output)) {
                return output;
            }
            return AccessModifier.None;
        }
    }
}