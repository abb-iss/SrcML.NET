using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// <para>AbstractCodeParser is used to parse SrcML files and extract useful info from the elements. Implementations of this class provide language-specific functions to extract useful data from the class.</para>
    /// <para>It contains two methods that wrap the language specific methods: <see cref="CreateTypeDefinition"/> and <see cref="CreateTypeUse"/></para>
    /// </summary>
    public abstract class AbstractCodeParser {
        protected AbstractCodeParser() {
        }

        /// <summary>
        /// Returns the Language that this parser supports
        /// </summary>
        public abstract Language ParserLanguage { get; }

        /// <summary>
        /// Returns the XNames that represent types for this language
        /// </summary>
        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary>
        /// Creates all of the type definitions from a file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit to search. <c>XElement.Name</c> must be SRC.Unit</param>
        /// <returns>An enumerable of TypeDefinition objects (one per type)</returns>
        public virtual IEnumerable<TypeDefinition> CreateTypeDefinitions(XElement fileUnit) {
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a a <unit> element", "fileUnit");

            Language language = SrcMLElement.GetLanguageForUnit(fileUnit);
            var fileName = GetFileNameForUnit(fileUnit);

            var typeElements = from typeElement in fileUnit.Descendants()
                               where TypeElementNames.Contains(typeElement.Name)
                               select CreateTypeDefinition(typeElement, fileUnit);
            return typeElements;
        }

        /// <summary>
        /// Parses the given typeElement and returns a TypeDefinition object.
        /// </summary>
        /// <param name="typeElement">the type XML element.</param>
        /// <returns>A new TypeDefinition object</returns>
        public virtual TypeDefinition CreateTypeDefinition(XElement typeElement, XElement fileUnit) {
            if(null == typeElement)
                throw new ArgumentNullException("typeElement");
            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a SRC.unit", "fileUnit");

            var typeDefinition = new TypeDefinition() {
               Accessibility = GetAccessModifierForType(typeElement),
               Filenames = new Collection<string>(),
               Kind = XNameMaps.GetKindForXElement(typeElement),
               Language = this.ParserLanguage,
               Name = GetNameForType(typeElement),
               Namespace = GetNamespaceDefinition(typeElement, fileUnit),
               Parents = GetParentTypeUses(typeElement, fileUnit),
               XPath = typeElement.GetXPath(false),
            };

            var fileName = GetFileNameForUnit(fileUnit);
            if(fileName.Length > 0) {
                typeDefinition.Filenames.Add(fileName);
            }

            typeDefinition.Namespace.Types.Add(typeDefinition);
            return typeDefinition;
        }

        /// <summary>
        /// Parses the type use and returns a TypeUse object
        /// </summary>
        /// <param name="element">An element naming the type. Must be a <see cref="ABB.SrcML.SRC.Type"/>or <see cref="ABB.SrcML.SRC.Name"/>.</param>
        /// <param name="fileUnit">The file unit that contains the typeElement</param>
        /// <returns>A new TypeUse object</returns>
        public virtual TypeUse CreateTypeUse(XElement element, XElement fileUnit) {
            XElement typeNameElement;
            string typeName = string.Empty;

            // validate the type use element (must be a SRC.Name or SRC.Type)
            if(element.Name == SRC.Type) {
                typeNameElement = element.Element(SRC.Name);
            } else if(element.Name == SRC.Name) {
                typeNameElement = element;
            } else {
                throw new ArgumentException("element should be of type type or name", "element");
            }

            if(null == fileUnit)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("must be a SRC.unit", "fileUnit");

            if(typeNameElement.Elements(SRC.Name).Count() > 0) {
                typeName = typeNameElement.Elements(SRC.Name).Last().Value;
            } else {
                typeName = typeNameElement.Value;
            }

            var typeUse = new TypeUse() {
                Name = typeName,
                CurrentNamespace = GetNamespaceDefinition(element, fileUnit),
                Parser = this,
            };
            return typeUse;
        }
        /// <summary>
        /// Creates a NamespaceDefinition object for the given element. This function looks for the namespace that contains <paramref name="element"/> and creates a definition based on that.
        /// </summary>
        /// <param name="element">the element</param>
        /// <param name="fileUnit">The file unit</param>
        /// <returns>a new NamespaceDefinition object</returns>
        public abstract NamespaceDefinition GetNamespaceDefinition(XElement element, XElement fileUnit);

        /// <summary>
        /// Gets the name for the type element
        /// </summary>
        /// <param name="typeElement">The type element to get the name for</param>
        /// <returns>The name of the type</returns>
        public abstract string GetNameForType(XElement typeElement);

        /// <summary>
        /// Gets the access modifier for the given type
        /// </summary>
        /// <param name="typeElement">The type element</param>
        /// <returns>The access modifier for the tyep.</returns>
        public abstract AccessModifier GetAccessModifierForType(XElement typeElement);

        /// <summary>
        /// Gets the parents for the given type.
        /// </summary>
        /// <param name="typeElement">the type element to get the parents for</param>
        /// <param name="fileUnit">the file unit that contains <paramref name="typeElement"/></param>
        /// <returns>A collection of TypeUses that represent the parent classes of <paramref name="typeElement"/></returns>
        public abstract Collection<TypeUse> GetParentTypeUses(XElement typeElement, XElement fileUnit);

        /// <summary>
        /// Get type aliases for the given file
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public abstract IEnumerable<Alias> CreateAliasesForFile(XElement fileUnit);

        /// <summary>
        /// Generates the possible names for this type use based on the aliases and the use data.
        /// </summary>
        /// <param name="typeUse">The type use to create</param>
        /// <returns>An enumerable of fully</returns>
        public abstract IEnumerable<string> GeneratePossibleNamesForTypeUse(TypeUse typeUse);

        /// <summary>
        /// Gets the filename for the given file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit. <c>fileUnit.Name</c> must be <c>SRC.Unit</c></param>
        /// <returns>The file path represented by this <paramref name="fileUnit"/></returns>
        public virtual string GetFileNameForUnit(XElement fileUnit) {
            if(fileUnit == null)
                throw new ArgumentNullException("fileUnit");
            if(fileUnit.Name != SRC.Unit)
                throw new ArgumentException("element must be a unit", "fileUnit");

            var fileNameAttribute = fileUnit.Attribute("filename");

            if(null != fileNameAttribute)
                return fileNameAttribute.Value;
            return String.Empty;
        }

        /// <summary>
        /// Gets all of the text nodes that are children of the given element.
        /// </summary>
        /// <param name="element">The element</param>
        /// <returns>An enumerable of the XText elements for <paramref name="element"/></returns>
        public IEnumerable<XText> GetTextNodes(XElement element) {
            var textNodes = from node in element.Nodes()
                where node.NodeType == XmlNodeType.Text
                let text = node as XText
                select text;
            return textNodes;
        }
    }
}
