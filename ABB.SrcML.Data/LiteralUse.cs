using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// TODO: explain what name is in this context
    /// </summary>
    public class LiteralUse : TypeUse {
        public LiteralKind Kind { get; set; }

        public static LiteralKind GetLiteralKind(XElement literalElement) {
            if(literalElement == null) throw new ArgumentNullException("literalElement");
            if(literalElement.Name != LIT.Literal) throw new ArgumentException("should be of type LIT.Literal", "literalElement");
            
            var typeAttribute = literalElement.Attribute("type");
            if(null == typeAttribute) throw new ArgumentException("should contain a \"type\" attribute", "literalElement");

            var kind = typeAttribute.Value;
            if(kind == "boolean")
                return LiteralKind.Boolean;
            else if(kind == "char")
                return LiteralKind.Character;
            else if(kind == "number")
                return LiteralKind.Number;
            else if(kind == "string")
                return LiteralKind.String;
            throw new SrcMLException(String.Format("\"{0}\" is not a valid literal kind", kind));
        }
    }
}
