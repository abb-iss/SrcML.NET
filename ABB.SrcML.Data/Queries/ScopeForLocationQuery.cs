using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Queries {
    public class ScopeForLocationQuery : AbstractQuery<SourceLocation, Statement> {
        public ScopeForLocationQuery(DataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory) { }

        public override Statement Execute(Statement globalScope, SourceLocation parameter) {
            //TODO reimplement once getscopeforlocation has been added
            //return globalScope.GetScopeForLocation(parameter);
            throw new NotImplementedException();
        }
    }

    public class ScopeForLocationQuery<TStatement>
        : AbstractQuery<SourceLocation, TStatement> where TStatement : Statement, new() {
        public ScopeForLocationQuery(DataRepository data, int lockTimeout, TaskFactory factory)
            : base(data, lockTimeout, factory){ }


        public override TStatement Execute(Statement globalScope, SourceLocation parameter) {
            //TODO reimplement once getscopeforlocation has been added
            //if(null != globalScope) {
            //    var scope = globalScope.GetScopeForLocation(parameter);
            //    return (scope != null ? scope.GetParentScopesAndSelf<TScope>().FirstOrDefault() : default(TScope));
            //}
            return default(TStatement);
        }
    }
}
