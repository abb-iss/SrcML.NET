using System;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Literal use is a specific kind of type use that refers to a language's built-in types.
    /// </summary>
    [Serializable]
    public class LiteralUse : TypeUse {

        /// <summary>
        /// The kind of literal
        /// </summary>
        public LiteralKind Kind { get; set; }

        /// <summary>
        /// Gets the literal kind from the
        /// <paramref name="literalElement"/></summary>
        /// <param name="literalElement">The literal element</param>
        /// <returns>The kind of element this is</returns>
        public static LiteralKind GetLiteralKind(XElement literalElement) {
            if(literalElement == null)
                throw new ArgumentNullException("literalElement");
            if(literalElement.Name != LIT.Literal)
                throw new ArgumentException("should be of type LIT.Literal", "literalElement");

            var typeAttribute = literalElement.Attribute("type");
            if(null == typeAttribute)
                throw new ArgumentException("should contain a \"type\" attribute", "literalElement");

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