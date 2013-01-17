using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    class TypeInventory {
        private Dictionary<string, Collection<TypeDefinition>> typeMap;

        public TypeInventory() {
            typeMap = new Dictionary<string, Collection<TypeDefinition>>();
        }

        public IEnumerable<TypeDefinition> ResolveType(TypeUse typeUse) {
            foreach(string fullName in typeUse.GetPossibleNames()) {
                Collection<TypeDefinition> results;
                if(this.typeMap.TryGetValue(fullName, out results)) {
                    foreach(var result in results) {
                        yield return result;
                    }
                }
            }
        }

        public void AddNewDefinition(TypeDefinition typeDefinition) {
            Collection<TypeDefinition> bucket;
            if(!this.typeMap.TryGetValue(typeDefinition.GetFullName(), out bucket)) {
                bucket = new Collection<TypeDefinition>();
            }
            bucket.Add(typeDefinition);
        }

    }
}
