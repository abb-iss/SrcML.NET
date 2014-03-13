using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Queries {
    public class ScopeForLocationQuery : AbstractQuery<SourceLocation, IScope> {
        public ScopeForLocationQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public override IScope Execute(IScope globalScope, SourceLocation parameter) {
            return globalScope.GetScopeForLocation(parameter);
        }
    }

    public class ScopeForLocationQuery<TScope>
        : AbstractQuery<SourceLocation, TScope> where TScope : IScope, new() {
        public ScopeForLocationQuery(IDataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory){ }


        public override TScope Execute(IScope globalScope, SourceLocation parameter) {
            if(null != globalScope) {
                var scope = globalScope.GetScopeForLocation(parameter);
                return (scope != null ? scope.GetParentScopesAndSelf<TScope>().FirstOrDefault() : default(TScope));
            }
            return default(TScope);
        }
    }
}
