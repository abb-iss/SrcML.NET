/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System.Collections.Generic;
namespace ABB.SrcML.Data {

    /// <summary>
    /// An enumeration of the different kinds of literals
    /// </summary>
    public enum LiteralKind {

        /// <summary>
        /// String literal
        /// </summary>
        String,

        /// <summary>
        /// Boolean literal
        /// </summary>
        Boolean,

        /// <summary>
        /// Character literal
        /// </summary>
        Character,

        /// <summary>
        /// Number literal
        /// </summary>
        Number,

        /// <summary>
        /// Null literal
        /// </summary>
        Null
    }

    /// <summary>
    /// Contains extension methods for the LiteralKind enum.
    /// </summary>
    public static class LiteralKindExtensions {
        /// <summary> Returns a keyword string for this LiteralKind. </summary>
        public static string ToKeyword(this LiteralKind lk) {
            switch(lk) {
                case LiteralKind.String:
                    return "String";
                case LiteralKind.Boolean:
                    return "Boolean";
                case LiteralKind.Character:
                    return "Character";
                case LiteralKind.Number:
                    return "Number";
                case LiteralKind.Null:
                    return "Null";
                default:
                    return string.Empty;
            }
        }

        /// <summary> Returns a LiteralKind for the given keyword string. </summary>
        public static LiteralKind FromKeyword(string keyword) {
            var mapping = new Dictionary<string, LiteralKind>() {
                {"String", LiteralKind.String},
                {"Boolean", LiteralKind.Boolean},
                {"Character", LiteralKind.Character},
                {"Number", LiteralKind.Number},
                {"Null", LiteralKind.Null}
            };

            LiteralKind output;
            if(!string.IsNullOrEmpty(keyword) && mapping.TryGetValue(keyword, out output)) {
                return output;
            }
            throw new SrcMLException("not a valid literal");
        }
    }
}