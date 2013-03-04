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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using NUnit.Framework;
using ABB.SrcML;
using SDML.SrcMLVSAddin.SyntaticCategory;
using System.IO;


namespace SDML.SrcMLVSAddin.SyntaticCategory.Test
{
    /// <summary>
    /// Summary description for SyntacticCategoryTest
    /// </summary>
    [TestFixture]
    [Category("External")]
    public class SyntacticCategoryTest
    {
        public IEnumerable<XElement> QueryForNew(XElement element)
        {
            var uses = from use in element.Descendants(OP.Operator)
                       where use.Value == "new"
                       select use;
            return uses;
        }

        [Test]
        public void BasicTest()
        {
            if(!File.Exists(Helper.NppXmlPath)) {
                Assert.Ignore(String.Format("SrcML for Notepad++ is not available at {0}", Helper.NppXmlPath));
            }
            var document = new SrcMLFile(Helper.NppXmlPath);

            var newUses = from unit in document.FileUnits
                          from use in QueryForNew(unit)
                          select use;

            SyntaticCategoryDataModel model = new SyntaticCategoryDataModel();
            
            foreach (var element in newUses)
            {
                var occurrence = new SyntaticOccurance(model, element);
            }

            //Console.WriteLine("{0} uses of the \"new\" operator in {1} categories", newUses.Count(), model.SyntaticCategories.Keys.Count);
            
            foreach (var category in model.SyntaticCategories.Keys)
            {
                
                var xpath = model.SyntaticCategories[category].First().CategoryAsXPath;//.Substring(1);

                var results = from use in newUses
                              let occurrence = new SyntaticOccurance(model, use)
                              where occurrence.CategoryAsXPath == xpath
                              select use;

                //Console.WriteLine("{0,3} uses of the new operator in {1}", results.Count(), xpath);
                Assert.AreEqual(model.SyntaticCategories[category].Count, results.Count(), category);
            }
        }
    }
}
