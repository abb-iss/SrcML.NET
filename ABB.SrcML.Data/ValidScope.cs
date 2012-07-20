/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data
{
    /// <summary>
    /// Class to represent valid scopes for definitions in the database
    /// </summary>
    partial class ValidScope
    {
        internal static Dictionary<Tuple<string, string>, string> MakeDictionary(string connectionString)
        {
            using (var db = new SrcMLDataContext(connectionString))
            {
                db.ObjectTrackingEnabled = false;
                db.CommandTimeout = 300;
                return MakeDictionary(db);
            }
        }

        internal static Dictionary<Tuple<string, string>, string> MakeDictionary(SrcMLDataContext db)
        {
            var scopes = from scope in db.ValidScopes
                         where scope.Definition.DefinitionTypeId == DefinitionType.DeclarationVariable
                         let declaration = scope.Definition as VariableDeclaration
                         select new KeyValuePair<Tuple<string, string>, string>(Tuple.Create(scope.XPath, declaration.DeclarationName), declaration.VariableTypeName);
            var scopeMap = scopes.ToDictionary(x => x.Key, x => x.Value);

            return scopeMap;
        }

        internal static Dictionary<Tuple<string, string>, string> MakeLocalDictionary(SrcMLDataContext db, XElement fileUnit)
        {
            var pathToFileUnit = fileUnit.GetXPath(false);
            var scopes = from scope in db.ValidScopes
                         where scope.Definition.DefinitionTypeId == DefinitionType.DeclarationVariable
                         where scope.XPath.StartsWith(pathToFileUnit)
                         let declaration = scope.Definition as VariableDeclaration
                         select new KeyValuePair<Tuple<string, string>, string>(
                             Tuple.Create(scope.XPath, declaration.DeclarationName),
                             declaration.VariableTypeName
                         );
            var scopeMap = scopes.ToDictionary(x => x.Key, x => x.Value);
            return scopeMap;
        }
    }
}
