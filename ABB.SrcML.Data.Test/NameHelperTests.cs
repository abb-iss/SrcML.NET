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

using NUnit.Framework;
using System;
using System.Linq;
using System.Xml.Linq;

namespace ABB.SrcML.Data.Test {

    [TestFixture]
    [Category("Build")]
    internal class NameHelperTests {

        [Test]
        public void TestGetLastNameElement() {
            var nameElement = FormatRootNameElement("<name>A</name><name>B</name>");

            var expectedLastName = nameElement.Elements(SRC.Name).Last();
            var lastName = NameHelper.GetLastNameElement(nameElement);

            Assert.AreSame(expectedLastName, lastName);
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetLastNameElement_Null() {
            NameHelper.GetLastName(null);
        }

        [Test]
        public void TestGetNameElementsExceptLast() {
            var nameElement = FormatRootNameElement("<name>A</name><name>B</name>");

            var expectedFirstName = nameElement.Elements(SRC.Name).First();
            var nestedNamesExceptLast = NameHelper.GetNameElementsExceptLast(nameElement);

            Assert.AreEqual(1, nestedNamesExceptLast.Count());
            Assert.AreSame(expectedFirstName, nestedNamesExceptLast.First());
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetNameElementsExceptLast_Null() {
            NameHelper.GetNameElementsExceptLast(null).First();
        }

        [Test]
        public void TestGetNestedNameElements() {
            var nameElement = FormatRootNameElement("<name>A</name><name>B</name>");

            var nestedNames = NameHelper.GetNameElementsFromName(nameElement);

            Assert.AreEqual("A", nestedNames.First().Value);
            Assert.AreEqual("B", nestedNames.Last().Value);
        }

        [Test]
        public void TestGetNestedNameElements_NoNestedNames() {
            var nameElement = FormatRootNameElement("A");

            var expectedFirstName = nameElement.Value;

            Assert.AreSame(nameElement, NameHelper.GetNameElementsFromName(nameElement).First());
            Assert.AreEqual("A", NameHelper.GetLastName(nameElement));
            Assert.AreEqual(0, NameHelper.GetNameElementsExceptLast(nameElement).Count());
        }

        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void TestGetNestedNameElements_Null() {
            NameHelper.GetNameElementsFromName(null).First();
        }

        private XElement FormatRootNameElement(string content) {
            return XElement.Parse(String.Format(@"<name xmlns=""{0}"">{1}</name>", SrcML.NamespaceManager.LookupNamespace("src"), content));
        }
    }
}