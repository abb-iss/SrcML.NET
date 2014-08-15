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
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// This class houses a mapping of <see cref="System.Xml.Linq.XName"/> to
    /// <see cref="TypeKind"/>/
    /// </summary>
    internal static class XNameMaps {

        /// <summary>
        /// gets the TypeKind for the given typeElement. The element must be of node type
        /// <see cref="ABB.SrcML.SRC.Struct"/>, <see cref="ABB.SrcML.SRC.StructDeclaration"/>, 
        /// <see cref="ABB.SrcML.SRC.Class"/>, <see cref="ABB.SrcML.SRC.ClassDeclaration"/>
        /// <see cref="ABB.SrcML.SRC.Union"/>, <see cref="ABB.SrcML.SRC.UnionDeclaration"/>, or
        /// <see cref="ABB.SrcML.SRC.Enum"/>
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The kind of the type element</returns>
        public static TypeKind GetKindForXElement(XElement typeElement) {
            if(null == typeElement) { throw new ArgumentNullException("typeElement"); }
            
            var map = new Dictionary<XName, TypeKind>() {
                { SRC.Struct, TypeKind.Struct },
                { SRC.StructDeclaration, TypeKind.Struct },
                { SRC.Class, TypeKind.Class },
                { SRC.ClassDeclaration, TypeKind.Class },
                { SRC.Union, TypeKind.Union },
                { SRC.UnionDeclaration, TypeKind.Union },
                { SRC.Enum, TypeKind.Enumeration },
            };

            TypeKind answer;
            if(map.TryGetValue(typeElement.Name, out answer)) {
                if(TypeKind.Class == answer) {
                    var typeAttribute = typeElement.Attribute("type");
                    if(null != typeAttribute && typeAttribute.Value == "interface") {
                        return TypeKind.Interface;
                    }
                }
                return answer;
            }
            throw new ArgumentException("element must be of type struct, struct_decl, class, class_decl, union, union_decl, or enum", "typeElement");
        }
    }
}