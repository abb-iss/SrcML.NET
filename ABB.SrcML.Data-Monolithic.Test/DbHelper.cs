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
using System.IO;
using System.Diagnostics;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test
{
    public class DbHelper
    {
        public const bool REBUILDIFEXISTS = true;

        public static void AddArchiveToDb(string xmlArchive, bool rebuildDbIfExists = REBUILDIFEXISTS)
        {
            var dbName = SrcMLDataContext.MakeDBName(xmlArchive);
            bool databaseAlreadyExists = SrcMLDataContext.DatabaseExists(dbName);
            
            if (rebuildDbIfExists || !databaseAlreadyExists)
            {
                using (var db = SrcMLDataContext.CreateDatabaseConnection(dbName, rebuildDbIfExists))
                {
                    db.CommandTimeout = 600;

                    var w = Stopwatch.StartNew();
                    db.Load(xmlArchive);
                    w.Stop();

                    Console.WriteLine("{0} to load {1} into the database", w.Elapsed, xmlArchive);
                }
            }
            else
            {
                Console.WriteLine("Database {0} already exists", dbName);
            }
        }

        public static void GetStatsFromDb(string xmlArchive)
        {
            var dbName = SrcMLDataContext.MakeDBName(xmlArchive);

            using (var db = SrcMLDataContext.CreateDatabaseConnection(dbName))
            {
                var archive = (from a in db.Archives
                               where a.Path == xmlArchive
                               select a).First();
                var definitions = from definition in db.Definitions
                                  select definition;
                var scopes = from scope in db.ValidScopes
                             select scope;

                int definitionCount = definitions.Count();
                int typeCount = definitions.OfType<TypeDefinition>().Count();
                int methodCount = definitions.OfType<MethodDefinition>().Count();
                int declarationCount = definitions.OfType<Declaration>().Count();
                int scopeCount = scopes.Count();

                Console.WriteLine("{0} definitions", definitionCount);
                Console.WriteLine("\t{0,7} type definitions", typeCount);
                Assert.IsTrue(definitionCount >= typeCount, "number of definitions should be greater than or equal to the number of types");

                Console.WriteLine("\t{0,7} method definitions", methodCount);
                Assert.IsTrue(definitionCount >= methodCount, "number of definitions should be greater than or equal to the number of types");

                Console.WriteLine("\t{0,7} declarations", declarationCount);
                Assert.IsTrue(definitionCount >= declarationCount, "number of definitions should be greater than or equal to the number of declarations");

                Console.WriteLine("\t{0,7} scopes", scopes.Count());
            }
        }
    }
}
