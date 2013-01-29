using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The type inventory contains maps of all the types as well as what namespaces they resolve to.
    /// </summary>
    public class TypeInventory {
        private Dictionary<string, NamespaceDefinition> namespaceMap;
        private Dictionary<string, Collection<TypeDefinition>> typeMap;
        
        public TypeInventory() {
            namespaceMap = new Dictionary<string, NamespaceDefinition>();
            typeMap = new Dictionary<string, Collection<TypeDefinition>>();
        }

        /// <summary>
        /// Finds all of the type definitions that can map to this type use.
        /// </summary>
        /// <param name="typeUse">The type use context to look for</param>
        /// <returns>An enumerable of type definitions that match the usage</returns>
        public IEnumerable<TypeDefinition> ResolveType(TypeUse typeUse) {
            if(null == typeUse)
                throw new ArgumentNullException("typeUse");

            Collection<TypeDefinition> results;
            if(this.typeMap.TryGetValue(typeUse.Name, out results)) {
                foreach(var result in results)
                    yield return result;
            }
        }

        /// <summary>
        /// Add the given type definitions to the inventory
        /// </summary>
        /// <param name="typeDefinitions">The type definitions to add</param>
        public void AddNewDefinitions(IEnumerable<TypeDefinition> typeDefinitions) {
            if(null == typeDefinitions)
                throw new ArgumentNullException("typeDefinitions");

            foreach(var definition in typeDefinitions) {
                this.AddNewDefinition(definition);
            }
        }

        /// <summary>
        /// Add a single type definition to the inventory
        /// </summary>
        /// <param name="typeDefinition">The type definition to add</param>
        public void AddNewDefinition(TypeDefinition typeDefinition) {
            NamespaceDefinition namespaceForType;
            Collection<TypeDefinition> bucket;

            if(this.namespaceMap.TryGetValue(typeDefinition.Namespace.Name, out namespaceForType)) {
                namespaceForType.Types.Add(typeDefinition);
                typeDefinition.Namespace = namespaceForType;
            } else {
                namespaceMap[typeDefinition.Namespace.Name] = typeDefinition.Namespace;
            }
            
            if(!this.typeMap.TryGetValue(typeDefinition.Name, out bucket)) {
                bucket = new Collection<TypeDefinition>();
                this.typeMap[typeDefinition.Name] = bucket;
            }
            bucket.Add(typeDefinition);
        }

    }
}
