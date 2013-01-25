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
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test
{
    public class TypeTester
    {
        public static void TestTypeUse(string xmlFilePath, List<string> testNames, int testCountPerName = 10)
        {
            Helper<TypeDefinition>.RunOnMap(xmlFilePath, Verify, FindUsesOfType, FindDefinitionsFromArchive, testNames, testCountPerName);
        }

        private static IEnumerable<XElement> FindUsesOfType(XElement unit, string typeName)
        {
            var results = from use in unit.Descendants(SRC.Type)
                          where use.Elements(SRC.Name).Any(n => n.Value == typeName)
                          select use;
            return results;
        }

        private static IEnumerable<TypeDefinition> FindDefinitionsFromArchive(SrcMLDataContext db, Archive archive, XElement element)
        {
            return archive.GetTypeForVariableName(db, element);
        }
        private static bool Verify(SrcMLDataContext db, TypeDefinition def, XElement use)
        {
            return def.TypeName == use.Elements(SRC.Name).Last().Value;
        }
    }
}
