using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    public class JavaCodeParser : AbstractCodeParser {
        public JavaCodeParser() {
            this.TypeElementNames = new HashSet<XName>(new XName[] { SRC.Class });
        }
        public override Language ParserLanguage {
            get { return Language.Java; }
        }

        public override NamespaceDefinition GetNamespaceDefinition(XElement fileUnit, XElement element) {
            var javaPackage = fileUnit.Descendants(SRC.Package).FirstOrDefault();

            if(null != javaPackage) {
                var namespaceNames = from name in javaPackage.Elements(SRC.Name)
                                     select name.Value;
                var namespaceName = string.Join(".", namespaceNames);
                var definition = new NamespaceDefinition() {
                    Name = namespaceName,
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
            Dictionary<string, AccessModifier> accessModifierMap = new Dictionary<string, AccessModifier>() {
                { "public", AccessModifier.Public },
                { "private", AccessModifier.Private },
                { "protected", AccessModifier.Protected },
            };

            var modifiers = from specifier in typeElement.Elements(SRC.Specifier)
                            where accessModifierMap.ContainsKey(specifier.Value)
                            select accessModifierMap[specifier.Value];
            return modifiers.FirstOrDefault();
        }

        public override Collection<TypeUse> GetParentTypeUses(XElement fileUnit, XElement typeDefinition) {
            Collection<TypeUse> parents = new Collection<TypeUse>();
            var superTag = typeDefinition.Element(SRC.Super);

            if(null != superTag) {
                var implementsTag = superTag.Element(SRC.Implements);
                if(null != implementsTag) {
                    var parentElements = implementsTag.Elements(SRC.Name);
                    foreach(var parentElement in parentElements) {
                        parents.Add(CreateTypeUse(fileUnit, parentElement));
                    }
                }
                
            }
            return parents;
        }
    }
}
