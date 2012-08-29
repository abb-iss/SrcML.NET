/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using NUnit.Framework;
using System.Reflection;

namespace ABB.SrcML.Test
{
    [TestFixture]
    [Category("Build")]
    public class TransformHarnessTest
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullTransformTypeTest()
        {
            TransformObjectHarness harness = new TransformObjectHarness(null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void NonTransformTypeTest()
        {
            TransformObjectHarness harness = new TransformObjectHarness(typeof(String));
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void TransformWithoutDefaultConstructor()
        {
            ITransform harness = new TransformObjectHarness(typeof(TransformWithPrivateConstructor));
        }

        [Test]
        public void InvalidQueryFunctionTest()
        {
            var tests = QueryHarness.CreateFromType(typeof(InvalidQueryFunctions));
            Assert.AreEqual(0, tests.Count());
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QueryFunctionWithNullType()
        {
            QueryHarness harness = new QueryHarness(null, "test2");
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void QueryFunctionWithNullMethod()
        {
            QueryHarness harness = new QueryHarness(typeof(EmptyClass), (MethodInfo)null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void QueryFunctionWithMissingMethod()
        {
            QueryHarness harness = new QueryHarness(typeof(InvalidQueryFunctions), "test3");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void QueryFunctionWithBadSignature()
        {
            QueryHarness harness = new QueryHarness(typeof(InvalidQueryFunctions), "test2");
        }

        [Test]
        public void NoQueriesTest()
        {
            var tests = QueryHarness.CreateFromType(typeof(EmptyClass));
            Assert.AreEqual(0, tests.Count());
        }

        [Test]
        public void StaticQueryCreationWithoutDefaultConstructorTest()
        {
            QueryHarness q = new QueryHarness(typeof(StaticVsNonStaticQueryFunctions), "StaticMyQuery");
            Assert.IsNotNull(q);
        }
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void QueryCreationWithoutDefaultConstructorTest()
        {
            QueryHarness q = new QueryHarness(typeof(StaticVsNonStaticQueryFunctions), "MyQuery");
        }
    }
}
