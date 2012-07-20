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
using ABB.SrcML;

namespace ABB.SrcML.Data.Test
{
    /// <summary>
    /// Summary description for DeclarationTester
    /// </summary>
    public class DeclarationTester
    {
        public static void TestLocalVariables(string xmlFilePath, List<string> testNames, int testCountPerName = 10)
        {
            Helper<VariableDeclaration>.RunOnMap(xmlFilePath, MatchesLocalVariable, FindUsesOf, FindDefinitionsForVariable, testNames, testCountPerName);
        }

        public static void TestGlobalVariables(string xmlFilePath, List<string> testNames, int testCountPerName = 10)
        {
            Helper<VariableDeclaration>.RunOnMap(xmlFilePath, MatchesGlobalVariable, FindUsesOf, FindDefinitionsForVariable, testNames, testCountPerName);
        }

        private static IEnumerable<XElement> FindUsesOf(XElement unit, string variableName)
        {
            var fileName = unit.Attribute("filename").Value;
            if (fileName.ToLower().EndsWith(".h"))
                return Enumerable.Empty<XElement>();

            var results = from use in unit.Descendants(SRC.Name)
                          where use.Value == variableName && !use.Ancestors(SRC.Declaration).Any()
                          select use;
            return results;
        }

        private static IEnumerable<VariableDeclaration> FindDefinitionsForVariable(SrcMLDataContext db, Archive archive, XElement element)
        {
            return archive.GetDeclarationForVariable(db, element);
        }

        private static bool MatchesGlobalVariable(SrcMLDataContext db, VariableDeclaration def, XElement use)
        {
            return def.DeclarationName == use.Value && (def.IsGlobal ?? false);
        }

        private static bool MatchesLocalVariable(SrcMLDataContext db, VariableDeclaration def, XElement use)
        {
            if (def.IsGlobal ?? false)
                return false;

            if (def.DeclarationName != use.Value)
                return false;
            
            var useXPath = use.GetXPath(false);
            var validScopes = from scope in db.ValidScopes
                              where scope.DefinitionId == def.Id
                              select scope;
            foreach (var scope in validScopes)
            {
                if (useXPath.StartsWith(scope.XPath))
                    return true;
            }

            var method = (from ancestor in use.Ancestors()
                          where ContainerNames.MethodDefinitions.Any(mn => mn == ancestor.Name)
                          select ancestor).FirstOrDefault();
            var classNameFromMethod = SrcMLHelper.GetClassNameForMethod(method);

            if (null == classNameFromMethod)
            {
                return false;
            }

            var classDef = from scope in def.ValidScopes.OfType<TypeDefinition>()
                           where scope.TypeName == classNameFromMethod.Value
                           select scope;
            if (classDef.Any())
            {
                return true;
            }

            return false;
        }
    }
}