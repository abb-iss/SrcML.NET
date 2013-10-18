/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

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