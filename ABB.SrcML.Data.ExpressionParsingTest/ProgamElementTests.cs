/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test
{
    [TestFixture]
    public class ProgamElementTests
    {
        [Test]
        public void TestSiblingsBeforeSelf() {
            var a = new VariableUse() {Name = "a"};
            var plus = new OperatorUse() {Text = "+"};
            var foo = new VariableUse() {Name = "foo"};
            var times = new OperatorUse() {Text = "*"};
            var b = new VariableUse() {Name = "b"};
            var exp = new Expression();
            exp.AddComponents(new Expression[] {a, plus, foo, times, b});

            var fooSiblings = foo.GetSiblingsBeforeSelf().ToList();
            Assert.AreEqual(2, fooSiblings.Count());
            Assert.AreSame(a, fooSiblings[0]);
            Assert.AreSame(plus, fooSiblings[1]);

            var aSiblings = a.GetSiblingsBeforeSelf().ToList();
            Assert.AreEqual(0, aSiblings.Count());
        }

        [Test]
        public void TestSiblingsBeforeSelf_MissingChild() {
            var a = new VariableUse() {Name = "a"};
            var plus = new OperatorUse() {Text = "+"};
            var foo = new VariableUse() {Name = "foo"};
            var times = new OperatorUse() {Text = "*"};
            var b = new VariableUse() {Name = "b"};
            var exp = new Expression();
            exp.AddComponents(new Expression[] {a, plus, foo, times, b});

            var dot = new OperatorUse {
                Text = ".",
                ParentExpression = exp
            };

            Assert.Throws<InvalidOperationException>(() => dot.GetSiblingsBeforeSelf());
        }

        [Test]
        public void TestSiblingsAfterSelf() {
            var a = new VariableUse() {Name = "a"};
            var plus = new OperatorUse() {Text = "+"};
            var foo = new VariableUse() {Name = "foo"};
            var times = new OperatorUse() {Text = "*"};
            var b = new VariableUse() {Name = "b"};
            var exp = new Expression();
            exp.AddComponents(new Expression[] {a, plus, foo, times, b});

            var plusSiblings = plus.GetSiblingsAfterSelf().ToList();
            Assert.AreEqual(3, plusSiblings.Count());
            Assert.AreSame(foo, plusSiblings[0]);
            Assert.AreSame(times, plusSiblings[1]);
            Assert.AreSame(b, plusSiblings[2]);

            var bSiblings = b.GetSiblingsAfterSelf().ToList();
            Assert.AreEqual(0, bSiblings.Count());
        }

        [Test]
        public void TestSiblingsAfterSelf_MissingChild() {
            var a = new VariableUse() {Name = "a"};
            var plus = new OperatorUse() {Text = "+"};
            var foo = new VariableUse() {Name = "foo"};
            var times = new OperatorUse() {Text = "*"};
            var b = new VariableUse() {Name = "b"};
            var exp = new Expression();
            exp.AddComponents(new Expression[] {a, plus, foo, times, b});

            var dot = new OperatorUse {
                Text = ".",
                ParentExpression = exp
            };

            Assert.Throws<InvalidOperationException>(() => dot.GetSiblingsAfterSelf());
        }
    }
}
