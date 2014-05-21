using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class NamePrefix : Expression {
        public NamePrefix() : base() { }

        public IEnumerable<NameUse> Names { get { return Components.OfType<NameUse>(); } }

        public IEnumerable<NamedScope> FindMatches(NamedScope root) {
            IEnumerable<NamedScope> candidates = null;

            var prefixes = Names.ToList();
            Dictionary<NameUse, List<NamedScope>> prefixMap = new Dictionary<NameUse, List<NamedScope>>();
            for(int i = 0; i < prefixes.Count; i++) {
                if(0 == i) {
                    prefixMap[prefixes[i]] = (from child in root.ChildStatements.OfType<NamedScope>()
                                              where child.Name == prefixes[i].Name
                                              select child).ToList();
                } else {
                    prefixMap[prefixes[i]] = (from candidate in prefixMap[prefixes[i - 1]]
                                              from child in candidate.ChildStatements.OfType<NamedScope>()
                                              where child.Name == prefixes[i].Name
                                              select child).ToList();
                }
            }
            return prefixMap[prefixes[prefixes.Count - 1]];
        }
    }
}
