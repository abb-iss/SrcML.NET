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

    public interface IMethodDefinition : INamedScope {

        bool IsConstructor { get; set; }

        bool IsDestructor { get; set; }

        ReadOnlyCollection<IParameterDeclaration> Parameters { get; }

        ITypeUse ReturnType { get; set; }

        void AddMethodParameter(IParameterDeclaration parameter);

        void AddMethodParameters(IEnumerable<IParameterDeclaration> parameters);

        bool ContainsCallTo(IMethodDefinition callee);

        IEnumerable<IMethodCall> GetCallsTo(IMethodDefinition callee);

        IEnumerable<IMethodCall> GetCallsToSelf();

        IEnumerable<IMethodCall> GetCallsToSelf(IScope rootScope);
    }
}