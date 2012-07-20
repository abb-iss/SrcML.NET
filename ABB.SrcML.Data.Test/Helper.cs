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
using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace ABB.SrcML.Data.Test
{
    delegate bool VerifyRelationship<TDef>(SrcMLDataContext db, TDef definition, XElement use) where TDef : Definition;
    delegate IEnumerable<XElement> FindNodes(XElement unit, string name);
    delegate IEnumerable<TDef> FindResults<TDef>(SrcMLDataContext db, Archive archive, XElement element) where TDef : Definition;

    static class Helper<TDef>
        where TDef : Definition
    {
        public static IEnumerable<XElement> FindUsesInDocument(SrcMLFile document, FindNodes filter, string name, int count)
        {
            var allResults = (from unit in document.FileUnits
                              let results = filter(unit, name)
                              select results).SelectMany(r => r);
            return allResults.Take(count);
        }

        public static void RunOnMap(string xmlFilePath, VerifyRelationship<TDef> verify, FindNodes testNameQuery, FindResults<TDef> searchArchive, List<string> testNames, int testCountPerName = 10)
        {
            var dbName = SrcMLDataContext.MakeDBName(xmlFilePath);

            SrcMLDataContext.DropUserInstanceDatabase(dbName);
            using (var db = SrcMLDataContext.CreateDatabaseConnection(dbName))
            {
                db.ObjectTrackingEnabled = false;
                db.CommandTimeout = 300;

                var archive = (from a in db.Archives
                               where a.Path == xmlFilePath
                               select a).FirstOrDefault();

                var sw = new Stopwatch();
                foreach (var testName in testNames)
                {
                    Console.WriteLine(testName);

                    var testUses = FindUsesInDocument(archive.Document, testNameQuery, testName, testCountPerName);
                    
                    foreach (var use in testUses)
                    {
                        var useFileName = use.Ancestors(SRC.Unit).First().Attribute("filename").Value;
                        var useLineNumber = use.GetSrcLineNumber();

                        sw.Restart();
                        var results = searchArchive(db, archive, use).ToList();
                        sw.Stop();
                        
                        Assert.IsNotNull(results);
                        Assert.AreNotEqual(0, results.Count());

                        Console.WriteLine("{0} to find {1} results", sw.Elapsed, results.Count());
                        foreach (var result in results)
                        {
                            Assert.IsTrue(verify(db, result, use), String.Format("\r\n{0}:{1} does not match\r\n{2}:{3}\r\n{4}", useFileName, useLineNumber, result.FileName, result.LineNumber, result.XPath));
                            Console.WriteLine("\t{0}:{1} matches {2}:{3}", useFileName, useLineNumber, result.FileName, result.LineNumber);
                        }
                    
                    }
                
                }
            }
        }
    }
}
