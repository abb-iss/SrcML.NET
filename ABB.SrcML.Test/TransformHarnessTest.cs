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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;

namespace ABB.SrcML.Test
{
    [TestClass]
    public class TransformHarnessTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "TransformObjectHarness should raise an exception when type is null")]
        public void NullTransformTypeTest()
        {
            TransformObjectHarness harness = new TransformObjectHarness(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "TransformObjectHarness should raise an exception when type does not implement ABB.SrcML.ITransform")]
        public void NonTransformTypeTest()
        {
            TransformObjectHarness harness = new TransformObjectHarness(typeof(String));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "TransformObjectHarness should raise an exception when type does not have a public default constructor")]
        public void TransformWithoutDefaultConstructor()
        {
            ITransform harness = new TransformObjectHarness(typeof(TransformWithPrivateConstructor));
        }

        [TestMethod]
        public void InvalidQueryFunctionTest()
        {
            var tests = QueryHarness.CreateFromType(typeof(InvalidQueryFunctions));
            Assert.AreEqual(0, tests.Count());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "QueryHarness should raise an exception when type is null")]
        public void QueryFunctionWithNullType()
        {
            QueryHarness harness = new QueryHarness(null, "test2");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "QueryHarness should raise an exception when method is null")]
        public void QueryFunctionWithNullMethod()
        {
            QueryHarness harness = new QueryHarness(typeof(EmptyClass), (MethodInfo)null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "QueryHarness should raise an exception when method is not found")]
        public void QueryFunctionWithMissingMethod()
        {
            QueryHarness harness = new QueryHarness(typeof(InvalidQueryFunctions), "test3");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "QueryHarness should raise an exception when the method has an invalid signature")]
        public void QueryFunctionWithBadSignature()
        {
            QueryHarness harness = new QueryHarness(typeof(InvalidQueryFunctions), "test2");
        }

        [TestMethod]
        public void NoQueriesTest()
        {
            var tests = QueryHarness.CreateFromType(typeof(EmptyClass));
            Assert.AreEqual(0, tests.Count());
        }

        [TestMethod]
        public void StaticQueryCreationWithoutDefaultConstructorTest()
        {
            QueryHarness q = new QueryHarness(typeof(StaticVsNonStaticQueryFunctions), "StaticMyQuery");
            Assert.IsNotNull(q);
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentException), "QueryHarness should raise an exception if method is not static and type does not have a public default constructor")]
        public void QueryCreationWithoutDefaultConstructorTest()
        {
            QueryHarness q = new QueryHarness(typeof(StaticVsNonStaticQueryFunctions), "MyQuery");
        }
    }
}
