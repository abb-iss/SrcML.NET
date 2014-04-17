using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Queries {
    public class ScopeForLocationQuery : AbstractQuery<SourceLocation, Scope> {
        public ScopeForLocationQuery(DataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        protected override Scope ExecuteImpl(SourceLocation parameter) {
            return Data.GetGlobalScope().GetScopeForLocation(parameter);
        }
    }

    public class ScopeForLocationQuery<TScope>
        : AbstractQuery<SourceLocation, TScope> where TScope : Scope, new() {
        public ScopeForLocationQuery(DataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory){ }


        protected override TScope ExecuteImpl(SourceLocation parameter) {
            var globalScope = Data.GetGlobalScope();
            if(null != globalScope) {
                var scope = globalScope.GetScopeForLocation(parameter);
                return (scope != null ? scope.GetParentScopesAndSelf<TScope>().FirstOrDefault() : default(TScope));
            }
            return default(TScope);
        }
    }
}
