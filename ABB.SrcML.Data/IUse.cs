using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IUse<DEFINITION>
     where DEFINITION : class {

        ReadOnlyCollection<Alias> Aliases { get; }

        SrcMLLocation Location { get; set; }

        string Name { get; set; }

        IScope ParentScope { get; set; }

        IEnumerable<IScope> ParentScopes { get; }

        ABB.SrcML.Language ProgrammingLanguage { get; set; }

        void AddAlias(Alias alias);

        void AddAliases(IEnumerable<Alias> aliasesToAdd);

        IEnumerable<DEFINITION> FindMatches();

        bool Matches(DEFINITION definition);
    }
}