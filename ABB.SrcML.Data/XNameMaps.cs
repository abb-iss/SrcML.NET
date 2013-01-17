using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    static class XNameMaps {
        /// <summary>
        /// gets the TypeKind for the given typeElement. The element must be of node type <see cref="ABB.SrcML.SRC.Struct"/>,
        /// <see cref="ABB.SrcML.SRC.Class"/>, <see cref="ABB.SrcML.SRC.Struct"/>, <see cref="ABB.SrcML.SRC.Union"/>,
        /// or <see cref="ABB.SrcML.SRC.Enumeration"/>
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The kind of the type element</returns>
        public static TypeKind GetKindForXElement(XElement typeElement) {
            Dictionary<XName, TypeKind> map = new Dictionary<XName, TypeKind>() {
                { SRC.Struct, TypeKind.Struct },
                { SRC.Class, TypeKind.Class },
                { SRC.Union, TypeKind.Union },
                { SRC.Enum, TypeKind.Enumeration },
            };

            if(null == typeElement) {
                throw new ArgumentNullException("typeElement");
            }

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
            throw new ArgumentException("element must be of type struct, class, union, or enum", "typeElement");
        }
    }
}
