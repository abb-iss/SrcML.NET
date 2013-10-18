using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Data {

    public interface IUse<DEFINITION> : IRootedObject
     where DEFINITION : class {

        ReadOnlyCollection<IAlias> Aliases { get; }

        SrcMLLocation Location { get; set; }

        string Name { get; set; }

        IEnumerable<IScope> ParentScopes { get; }

        ABB.SrcML.Language ProgrammingLanguage { get; set; }

        void AddAlias(IAlias alias);

        void AddAliases(IEnumerable<IAlias> aliasesToAdd);

        IEnumerable<DEFINITION> FindMatches();

        bool Matches(DEFINITION definition);
    }
}