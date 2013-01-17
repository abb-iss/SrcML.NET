using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using KsuAdapter = ABB.SrcML.Utilities.KsuAdapter;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The SourceFileParser class generates data from srcML files
    /// </summary>
    /// TODO configure refactoring this to separate the different languages into different classes
    public class CPlusPlusCodeParser : AbstractCodeParser {
        public CPlusPlusCodeParser() {
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class, SRC.Struct, SRC.Union });
        }
        public override Language ParserLanguage {
            get { return Language.CPlusPlus; }
        }

        public static TypeUse CreateTypeUse(XElement declarationStatementElement) {
            throw new NotImplementedException();
        }

        public override NamespaceDefinition GetNamespaceDefinition(XElement fileUnit, XElement element) {
            var names = from namespaceElement in element.Ancestors(SRC.Namespace)
                        let name = namespaceElement.Element(SRC.Name)
                        select name.Value;

            var namespaceName = String.Join(".", names.Reverse());

            if(namespaceName.Length > 0) {
                NamespaceDefinition definition = new NamespaceDefinition() {
                    Name = namespaceName
                };

                return definition;
            }
            return null;
        }

        public override string GetNameForType(XElement typeElement) {
            var name = typeElement.Element(SRC.Name);
            if(null == name)
                return string.Empty;
            return name.Value;
        }

        public override AccessModifier GetAccessModifierForType(XElement typeElement) {
            return AccessModifier.Public;
        }

        public override TypeUse CreateTypeUse(XElement fileUnit, XElement element) {
            throw new NotImplementedException();
        }

        public override Collection<TypeUse> GetParentTypeUses(XElement fileUnit, XElement typeDefinition) {
            Collection<TypeUse> parents = new Collection<TypeUse>();
            var superTag = typeDefinition.Element(SRC.Super);

            if(null != superTag) {
                var parentElements = superTag.Elements(SRC.Name);
                foreach(var parentElement in parentElements) {
                    parents.Add(CreateTypeUse(fileUnit, parentElement));
                }
            }
            return parents;
        }
    }
}
