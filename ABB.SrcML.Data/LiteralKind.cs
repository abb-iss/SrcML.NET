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
    }

    public static class LiteralKindExtensions {
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
                default:
                    return string.Empty;
            }
        }

        public static LiteralKind FromKeyword(string keyword) {
            Dictionary<string, LiteralKind> mapping = new Dictionary<string, LiteralKind>() {
                { "String", LiteralKind.String },
                { "Boolean", LiteralKind.Boolean },
                { "Character", LiteralKind.Character },
                { "Number", LiteralKind.Number },
        };

            LiteralKind output;
            if(!string.IsNullOrEmpty(keyword) && mapping.TryGetValue(keyword, out output)) {
                return output;
            }
            throw new SrcMLException("not a valid literal");
        }
    }
}