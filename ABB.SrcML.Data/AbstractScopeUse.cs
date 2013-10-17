using System;
using System.Collections.Generic;
using System.Linq;

namespace ABB.SrcML.Data {

    /// <summary>
    /// Abstract Scope Use is a variable use for Named Scopes. It provides an implementation of
    /// <see cref="AbstractUse{T}.FindMatches()"/> for resolving Named Scope uses
    /// </summary>
    /// <typeparam name="DEFINITION">The type. Must be NamedScope or a subclass</typeparam>
    [Serializable]
    public abstract class AbstractScopeUse<DEFINITION> : AbstractUse<DEFINITION>
        where DEFINITION : NamedScope {

        /// <summary>
        /// Finds matching <typeparamref name="DEFINITION"/> from the
        /// <see cref="AbstractUse{T}.ParentScopes"/> of this usage.
        /// </summary>
        /// <returns>An enumerable of <typeparamref name="DEFINITION"/> objects that
        /// <see cref="AbstractUse{T}.Matches">matches</see> this usage.</returns>
        public override IEnumerable<DEFINITION> FindMatches() {
            DEFINITION definition = null;
            foreach(var parent in this.ParentScopes) {
                definition = parent as DEFINITION;

                if(Matches(definition)) {
                    yield return definition;
                }

                var matchingChildren = from child in parent.GetChildScopesWithId<DEFINITION>(this.Name)
                                       where Matches(child)
                                       select child;

                foreach(var match in matchingChildren) {
                    yield return match;
                }
            }

            NamespaceDefinition globalScope = this.ParentScope.GetParentScopesAndSelf<NamespaceDefinition>().Where(n => n.IsGlobal).FirstOrDefault();

            if(globalScope != null) {
                foreach(var alias in Aliases) {
                    if(alias.IsNamespaceImport) {
                        var answers = from aliasedNamespace in alias.FindMatchingNamespace(globalScope)
                                      from matchingScope in aliasedNamespace.GetChildScopesWithId<DEFINITION>(this.Name)
                                      where this.Matches(matchingScope)
                                      select matchingScope;

                        foreach(var answer in answers) {
                            yield return answer;
                        }
                    }
                }
            }
        }
    }
}