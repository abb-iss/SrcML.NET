using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public abstract class AbstractCodeParser {
        protected AbstractCodeParser() {
        }

        public abstract Language ParserLanguage { get; }

        public HashSet<XName> TypeElementNames { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
        public virtual IEnumerable<TypeDefinition> CreateTypeDefinitions(XElement fileUnit) {
            Language language = SrcMLElement.GetLanguageForUnit(fileUnit);
            var fileName = GetFileNameForUnit(fileUnit);

            var typeElements = from typeElement in fileUnit.Descendants()
                               where TypeElementNames.Contains(typeElement.Name)
                               select CreateTypeDefinition(typeElement, fileUnit);
            return typeElements;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeElement"></param>
        /// <returns></returns>
        public virtual TypeDefinition CreateTypeDefinition(XElement typeElement, XElement fileUnit) {
            var typeDefinition = new TypeDefinition() {
               Accessibility = GetAccessModifierForType(typeElement),
               Filenames = new Collection<string>(),
               Kind = XNameMaps.GetKindForXElement(typeElement),
               Language = this.ParserLanguage,
               Name = GetNameForType(typeElement),
               Namespace = GetNamespaceDefinition(fileUnit, typeElement),
               Parents = GetParentTypeUses(fileUnit, typeElement),
               XPath = typeElement.GetXPath(false),
            };

            var fileName = GetFileNameForUnit(fileUnit);
            if(fileName.Length > 0) {
                typeDefinition.Filenames.Add(fileName);
            }
            return typeDefinition;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual TypeUse CreateTypeUse(XElement fileUnit, XElement element) {
            XElement typeNameElement;
            string typeName = string.Empty;

            if(element.Name == SRC.Type) {
                typeNameElement = element.Element(SRC.Name);
            } else if(element.Name == SRC.Name) {
                typeNameElement = element;
            } else {
                throw new ArgumentException("element should be of type type or name", "element");
            }

            if(typeNameElement.Elements(SRC.Name).Count() > 0) {
                typeName = typeNameElement.Elements(SRC.Name).Last().Value;
            } else {
                typeName = typeNameElement.Value;
            }

            var typeUse = new TypeUse() {
                Name = typeName,
            };
            return typeUse;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public abstract NamespaceDefinition GetNamespaceDefinition(XElement fileUnit, XElement element);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeElement"></param>
        /// <returns></returns>
        public abstract string GetNameForType(XElement typeElement);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="typeElement"></param>
        /// <returns></returns>
        public abstract AccessModifier GetAccessModifierForType(XElement typeElement);

        public abstract Collection<TypeUse> GetParentTypeUses(XElement fileUnit, XElement typeDefinition);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileUnit"></param>
        /// <returns></returns>
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
    }
}
