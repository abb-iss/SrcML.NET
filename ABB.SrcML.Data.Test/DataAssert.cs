/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - initial API, implementation, & documentation
 *****************************************************************************/

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ABB.SrcML.Data.Test {
    /// <summary>
    /// The DataAssert class provides methods for comparing two similar elements (<see cref="ABB.SrcML.Data.Statement"/>,
    /// <see cref="ABB.SrcML.Data.Expression"/>, or <see cref="ABB.SrcML.Data.SrcMLLocation"/>).
    /// </summary>
    public class DataAssert {
        /// <summary>
        /// Tests that two expressions are identical
        /// </summary>
        /// <param name="expected">The expected expression</param>
        /// <param name="actual">The actual expression</param>
        public static void ExpressionsAreEqual(Expression expected, Expression actual) {
            ExpressionsAreEqual(expected, actual, String.Empty);
        }

        /// <summary>
        /// Tests that two locations are identical
        /// </summary>
        /// <param name="expected">The expected location</param>
        /// <param name="actual">The actual location</param>
        public static void LocationsAreEqual(SrcMLLocation expected, SrcMLLocation actual) {
            LocationsAreEqual(expected, actual, string.Empty);
        }

        /// <summary>
        /// Tests that two statements are identical
        /// </summary>
        /// <param name="expected">The expected statement</param>
        /// <param name="actual">The actual statement</param>
        public static void StatementsAreEqual(Statement expected, Statement actual) {
            StatementsAreEqual(expected, actual, expected.GetXmlName());
        }

        private static void LocationsAreEqual(SrcMLLocation expected, SrcMLLocation actual, string propertyName) {
            if(expected != actual) {
                try {
                    IsTrue(expected != null, "expected!null");
                    IsTrue(actual != null, "actual!null");
                    IsTrue(expected.IsReference == actual.IsReference, "IsReference");
                    IsTrue(expected.SourceFileName == actual.SourceFileName, "SourceFileName");
                    IsTrue(expected.StartingLineNumber == actual.StartingLineNumber, "StartingLineNumber");
                    IsTrue(expected.StartingColumnNumber == actual.StartingColumnNumber, "StartingColumnNumber");
                    IsTrue(expected.EndingLineNumber == actual.EndingLineNumber, "EndingLineNumber");
                    IsTrue(expected.EndingColumnNumber == actual.EndingColumnNumber, "EndingColumnNumber");
                    IsTrue(expected.XPath == actual.XPath, "XPath");
                } catch(DataAssertionException e) {
                    e.Add(propertyName);
                    throw e;
                }
            }
        }

        private static void IsTrue(bool condition, string propertyName) {
            if(!condition) {
                var exception = new DataAssertionException();
                exception.Add(propertyName);
                throw exception;
            }
        }

        #region Collection equality methods
        private static void OrderedCollectionsAreEqual<T>(ICollection<T> expected, ICollection<T> actual, Action<T, T> test, string propertyName) {
            IsTrue(expected.Count == actual.Count, string.Format("{0}(count)", propertyName));
            for(int i = 0; i < actual.Count; i++) {
                try {
                    test(expected.ElementAt(i), actual.ElementAt(i));
                } catch(DataAssertionException e) {
                    e.Add(String.Format("{0}[{1}]", propertyName, i));
                    throw e;
                }
            }
        }

        private static void UnorderedCollectionsAreEqual<T>(ICollection<T> expected, ICollection<T> actual, Action<T, T> test, string propertyName) {
            IsTrue(expected.Count == actual.Count, string.Format("{0}(count)", propertyName));

            int successCount = 0;
            var matched = new bool[expected.Count];
            for(int i = 0; i < expected.Count; i++) {
                successCount = 0;
                for(int j = 0; j < expected.Count; j++) {
                    try {
                        test(expected.ElementAt(i), actual.ElementAt(j));
                        successCount++;
                    } catch(DataAssertionException) {

                    }
                }

                if(successCount > 0) {
                    matched[i] = true;
                } else {
                    var e = new DataAssertionException();
                    e.Add(String.Format("{0}[{1}]", propertyName, i));
                    throw e;
                }
            }

            for(int i = 0; i < expected.Count; i++) {
                if(!matched[i]) {
                    var e = new DataAssertionException();
                    e.Add(String.Format("{0}[{1}]", propertyName, i));
                    throw e;
                }
            }
        }
        #endregion Collection equality methods
        #region Statement equality methods

        private static void StatementsAreEqual(Statement expected, Statement actual, string propertyName) {
            if(expected != actual) {
                try {
                    IsTrue(expected != null, "a!null");
                    IsTrue(actual != null, "b!null");
                    IsTrue(expected.GetType() == actual.GetType(), "TYPE");
                } catch(DataAssertionException e) {
                    e.Add(propertyName);
                    throw e;
                }
                TestEquality((dynamic) expected, (dynamic) actual, propertyName);
            }
        }

        private static void TestEquality(Statement expected, Statement actual, string propertyName) {
            try {
                IsTrue(expected.ProgrammingLanguage == actual.ProgrammingLanguage, "ProgrammingLanguage");
                ExpressionsAreEqual(expected.Content, actual.Content, "Content");
                UnorderedCollectionsAreEqual<SrcMLLocation>(expected.Locations, actual.Locations, LocationsAreEqual, "Locations");
                OrderedCollectionsAreEqual<Statement>(expected.ChildStatements, actual.ChildStatements, StatementsAreEqual, "ChildStatements");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
        }

        private static void TestEquality(ImportStatement expected, ImportStatement actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.ImportedNamespace, actual.ImportedNamespace, "ImportedNamespace");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Statement) expected, (Statement) actual, propertyName);
        }

        private static void TestEquality(AliasStatement expected, AliasStatement actual, string propertyName) {
            try {
                IsTrue(expected.AliasName == actual.AliasName, "AliasName");
                ExpressionsAreEqual(expected.Target, actual.Target, "Target");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Statement) expected, (Statement) actual, propertyName);
        }

        private static void TestEquality(LabelStatement expected, LabelStatement actual, string propertyName) {
            try {
                IsTrue(expected.Name == actual.Name, "Name");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Statement) expected, (Statement) actual, propertyName);
        }

        private static void TestEquality(ExternStatement expected, ExternStatement actual, string propertyName) {
            try {
                IsTrue(expected.LinkageType == actual.LinkageType, "LinkageType");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Statement) expected, (Statement) actual, propertyName);
        }

        private static void TestEquality(BlockStatement expected, BlockStatement actual, string propertyName) {
            TestEquality((Statement) expected, (Statement) actual, propertyName);
        }

        private static void TestEquality(UsingBlockStatement expected, UsingBlockStatement actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.Initializer, actual.Initializer, "Initializer");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((BlockStatement) expected, (BlockStatement) actual, propertyName);
        }

        private static void TestEquality(TryStatement expected, TryStatement actual, string propertyName) {
            try {
                OrderedCollectionsAreEqual<CatchStatement>(expected.CatchStatements, actual.CatchStatements, StatementsAreEqual, "CatchStatements");
                OrderedCollectionsAreEqual<Statement>(expected.FinallyStatements, actual.FinallyStatements, StatementsAreEqual, "FinallyStatements");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((BlockStatement) expected, (BlockStatement) actual, propertyName);
        }

        private static void TestEquality(CatchStatement expected, CatchStatement actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.Parameter, actual.Parameter, "Parameter");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((BlockStatement) expected, (BlockStatement) actual, propertyName);
        }

        private static void TestEquality(NamedScope expected, NamedScope actual, string propertyName) {
            try {
                IsTrue(expected.Name == actual.Name, "Name");
                ExpressionsAreEqual(expected.Prefix, actual.Prefix, "Prefix");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((BlockStatement) expected, (BlockStatement) actual, propertyName);
        }

        private static void TestEquality(TypeDefinition expected, TypeDefinition actual, string propertyName) {
            try {
                IsTrue(expected.IsPartial == actual.IsPartial, "IsPartial");
                IsTrue(expected.Kind == actual.Kind, "Kind");
                UnorderedCollectionsAreEqual<TypeUse>(expected.ParentTypeNames, actual.ParentTypeNames, ExpressionsAreEqual, "ParentTypes");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NamedScope) expected, (NamedScope) actual, propertyName);
        }

        private static void TestEquality(MethodDefinition expected, MethodDefinition actual, string propertyName) {
            try {
                IsTrue(expected.IsConstructor == actual.IsConstructor, "IsConstructor");
                IsTrue(expected.IsDestructor == actual.IsDestructor, "IsDestructor");
                IsTrue(expected.IsPartial == actual.IsPartial, "IsPartial");
                ExpressionsAreEqual(expected.ReturnType, actual.ReturnType, "ReturnType");
                OrderedCollectionsAreEqual<VariableDeclaration>(expected.Parameters, actual.Parameters, ExpressionsAreEqual, "Parameters");
                OrderedCollectionsAreEqual<MethodCall>(expected.ConstructorInitializers, actual.ConstructorInitializers, ExpressionsAreEqual, "ConstructorInitializers");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NamedScope) expected, (NamedScope) actual, propertyName);
        }

        private static void TestEquality(PropertyDefinition expected, PropertyDefinition actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.ReturnType, actual.ReturnType, "ReturnType");
                StatementsAreEqual(expected.Getter, actual.Getter, "Getter");
                StatementsAreEqual(expected.Setter, actual.Setter, "Setter");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NamedScope) expected, (NamedScope) actual, propertyName);
        }

        private static void TestEquality(ConditionBlockStatement expected, ConditionBlockStatement actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.Condition, actual.Condition, "Condition");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((BlockStatement) expected, (BlockStatement) actual, propertyName);
        }

        private static void TestEquality(IfStatement expected, IfStatement actual, string propertyName) {
            try {
                OrderedCollectionsAreEqual<Statement>(expected.ElseStatements, actual.ElseStatements, StatementsAreEqual, "ElseStatements");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((ConditionBlockStatement) expected, (ConditionBlockStatement) actual, propertyName);
        }

        private static void TestEquality(CaseStatement expected, CaseStatement actual, string propertyName) {
            try {
                IsTrue(expected.IsDefault == actual.IsDefault, "IsDefault");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((ConditionBlockStatement) expected, (ConditionBlockStatement) actual, propertyName);
        }

        private static void TestEquality(ForStatement expected, ForStatement actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.Initializer, actual.Initializer, "Initializer");
                ExpressionsAreEqual(expected.Incrementer, actual.Incrementer, "Incrementer");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((ConditionBlockStatement) expected, (ConditionBlockStatement) actual, propertyName);
        }
        #endregion Statement equality methods
        #region Expression equality methods

        private static void ExpressionsAreEqual(Expression expected, Expression actual, string propertyName) {
            if(expected != actual) {
                try {
                    IsTrue(expected != null, "a!null");
                    IsTrue(actual != null, "b!null");
                    IsTrue(expected.GetType() == actual.GetType(), "TYPE");
                } catch(DataAssertionException e) {
                    e.Add(propertyName);
                    throw e;
                }
            }
        }

        private static void TestEquality(Expression expected, Expression actual, string propertyName) {
            try {
                IsTrue(expected.ProgrammingLanguage == actual.ProgrammingLanguage, "ProgrammingLanguage");
                LocationsAreEqual(expected.Location, actual.Location, "Location");
                OrderedCollectionsAreEqual<Expression>(expected.Components, actual.Components, ExpressionsAreEqual, "Components");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
        }

        private static void TestEquality(OperatorUse expected, OperatorUse actual, string propertyName) {
            try {
                IsTrue(expected.Text == actual.Text, "Text");
                ExpressionsAreEqual((Expression) expected, (Expression) actual, propertyName);
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
        }

        private static void TestEquality(VariableDeclaration expected, VariableDeclaration actual, string propertyName) {
            try {
                IsTrue(expected.Name == actual.Name, "Name");
                ExpressionsAreEqual(expected.Initializer, actual.Initializer, "Initializer");
                ExpressionsAreEqual(expected.Range, actual.Range, "Range");
                ExpressionsAreEqual(expected.VariableType, actual.VariableType, "VariableType");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Expression) expected, (Expression) actual, propertyName);
        }

        private static void TestEquality(LiteralUse expected, LiteralUse actual, string propertyName) {
            try {
                IsTrue(expected.Kind == actual.Kind, "Kind");
                IsTrue(expected.Text == actual.Text, "Value");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Expression) expected, (Expression) actual, propertyName);
        }

        private static void TestEquality(NameUse expected, NameUse actual, string propertyName) {
            try {
                IsTrue(expected.Name == actual.Name, "Name");
                ExpressionsAreEqual(expected.Prefix, actual.Prefix, "Prefix");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((Expression) expected, (Expression) actual, propertyName);
        }

        private static void TestEquality(TypeUse expected, TypeUse actual, string propertyName) {
            try {
                OrderedCollectionsAreEqual<TypeUse>(expected.TypeParameters, actual.TypeParameters, ExpressionsAreEqual, "TypeParameters");

            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NameUse) expected, (NameUse) actual, propertyName);
        }

        private static void TestEquality(MethodCall expected, MethodCall actual, string propertyName) {
            try {
                IsTrue(expected.IsConstructor == actual.IsConstructor, "IsConstructor");
                IsTrue(expected.IsDestructor == actual.IsDestructor, "IsDestructor");
                IsTrue(expected.IsConstructorInitializer == actual.IsConstructorInitializer, "IsConstructorInitializer");
                OrderedCollectionsAreEqual<Expression>(expected.Arguments, actual.Arguments, ExpressionsAreEqual, "Arguments");
                OrderedCollectionsAreEqual<TypeUse>(expected.TypeArguments, actual.TypeArguments, ExpressionsAreEqual, "TypeArguments");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NameUse) expected, (NameUse) actual, propertyName);
        }

        private static void TestEquality(VariableUse expected, VariableUse actual, string propertyName) {
            try {
                ExpressionsAreEqual(expected.Index, actual.Index, "Index");
            } catch(DataAssertionException e) {
                e.Add(propertyName);
                throw e;
            }
            TestEquality((NameUse) expected, (NameUse) actual, propertyName);
        }
        #endregion Expression equality methods
    }

    public class DataAssertionException : AssertionException {
        private string messagePrefix;
        public Stack<string> ErrorStack { get; set; }

        public override string Message {
            get {
                return FormatMessage();
            }
        }
        public DataAssertionException()
            : base("Data Assertion Failed") {
            messagePrefix = base.Message;
            ErrorStack = new Stack<string>();
        }

        public void Add(string text) {
            if(!String.IsNullOrEmpty(text)) {
                ErrorStack.Push(text);
            }
        }

        private string FormatMessage() {
            return String.Format("{0}: {1}", messagePrefix, String.Join(" / ", ErrorStack));
        }
    }
}
