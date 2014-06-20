using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    public static class TestHelper {

        public static bool StatementsAreEqual(Statement a, Statement b) {
            if(a == b) { return true; }
            if(a.GetType() != b.GetType()) { return false; }
            return TestEquality((dynamic) a, (dynamic) b);
        }

        public static bool ExpressionsAreEqual(Expression a, Expression b) {
            if(a == b) { return true; }
            if(a.GetType() != b.GetType()) { return false; }
            return TestEquality((dynamic) a, (dynamic) b);
        }

        public static bool LocationsAreEqual(SrcMLLocation a, SrcMLLocation b) {
            if(a == b) { return true; }
            return a.IsReference == b.IsReference &&
                   a.StartingColumnNumber == b.StartingColumnNumber &&
                   a.SourceFileName == b.SourceFileName &&
                   a.StartingLineNumber == b.StartingLineNumber &&
                   a.XPath == b.XPath;
        }


        /// <summary>
        /// Checks whether two collections have the same contents. The ordering is ignored.
        /// </summary>
        private static bool CollectionsAreEqual<T>(ICollection<T> a, ICollection<T> b, Func<T, T, bool> equalityComparer) {
            if(a == b) { return true; }
            bool equal = a.Count == b.Count;
            foreach(var aMember in a) {
                equal = equal && b.Any(bMember => equalityComparer(aMember, bMember));
            }
            foreach(var bMember in b) {
                equal = equal && a.Any(aMember => equalityComparer(bMember, aMember));
            }
            return equal;
        }

        /// <summary>
        /// Checks whether two collections have the same contents in the same order.
        /// </summary>
        private static bool OrderedCollectionsAreEqual<T>(ICollection<T> a, ICollection<T> b, Func<T, T, bool> equalityComparer) {
            if(a == b) { return true; }
            bool equal = a.Count == b.Count;
            equal = a.Zip(b, equalityComparer).Aggregate(equal, (current, result) => current && result);
            return equal;
        }

        #region Statement equality methods
        private static bool TestEquality(Statement a, Statement b) {
            return a.ProgrammingLanguage == b.ProgrammingLanguage &&
                   CollectionsAreEqual(a.Locations, b.Locations, LocationsAreEqual) &&
                   ExpressionsAreEqual(a.Content, b.Content) &&
                   OrderedCollectionsAreEqual(a.ChildStatements, b.ChildStatements, StatementsAreEqual);
        }

        private static bool TestEquality(ImportStatement a, ImportStatement b) {
            return ExpressionsAreEqual(a.ImportedNamespace, b.ImportedNamespace) &&
                   TestEquality((Statement)a, (Statement)b);
        }

        private static bool TestEquality(AliasStatement a, AliasStatement b) {
            return a.AliasName == b.AliasName &&
                   ExpressionsAreEqual(a.Target, b.Target) &&
                   TestEquality((Statement)a, (Statement)b);
        }

        private static bool TestEquality(LabelStatement a, LabelStatement b) {
            return a.Name == b.Name &&
                   TestEquality((Statement)a, (Statement)b);
        }

        private static bool TestEquality(ExternStatement a, ExternStatement b) {
            return a.LinkageType == b.LinkageType &&
                   TestEquality((Statement)a, (Statement)b);
        }

        private static bool TestEquality(BlockStatement a, BlockStatement b) {
            return TestEquality((Statement)a, (Statement)b);
        }

        private static bool TestEquality(UsingBlockStatement a, UsingBlockStatement b) {
            return ExpressionsAreEqual(a.Initializer, b.Initializer) &&
                   TestEquality((BlockStatement)a, (BlockStatement)b);
        }

        private static bool TestEquality(TryStatement a, TryStatement b) {
            return OrderedCollectionsAreEqual(a.CatchStatements, b.CatchStatements, StatementsAreEqual) &&
                   OrderedCollectionsAreEqual(a.FinallyStatements, b.FinallyStatements, StatementsAreEqual) &&
                   TestEquality((BlockStatement)a, (BlockStatement)b);
        }

        private static bool TestEquality(CatchStatement a, CatchStatement b) {
            return ExpressionsAreEqual(a.Parameter, b.Parameter) &&
                   TestEquality((BlockStatement)a, (BlockStatement)b);
        }

        private static bool TestEquality(NamedScope a, NamedScope b) {
            return a.Name == b.Name &&
                   ExpressionsAreEqual(a.Prefix, b.Prefix) &&
                   TestEquality((BlockStatement) a, (BlockStatement) b);
            //Accessibility is not tested because it's not merged/unmerged losslessly
        }
        
        private static bool TestEquality(TypeDefinition a, TypeDefinition b) {
            return a.IsPartial == b.IsPartial &&
                   a.Kind == b.Kind &&
                   CollectionsAreEqual(a.ParentTypes, b.ParentTypes, ExpressionsAreEqual) &&
                   TestEquality((NamedScope) a, (NamedScope) b);
        }

        private static bool TestEquality(MethodDefinition a, MethodDefinition b) {
            return a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   a.IsPartial == b.IsPartial &&
                   ExpressionsAreEqual(a.ReturnType, b.ReturnType) &&
                   OrderedCollectionsAreEqual(a.Parameters, b.Parameters, ExpressionsAreEqual) &&
                   TestEquality((NamedScope) a, (NamedScope) b);
        }

        private static bool TestEquality(PropertyDefinition a, PropertyDefinition b) {
            return ExpressionsAreEqual(a.ReturnType, b.ReturnType) &&
                   StatementsAreEqual(a.Getter, b.Getter) &&
                   StatementsAreEqual(a.Setter, b.Setter) &&
                   TestEquality((NamedScope)a, (NamedScope)b);
        }

        private static bool TestEquality(ConditionBlockStatement a, ConditionBlockStatement b) {
            return ExpressionsAreEqual(a.Condition, b.Condition) &&
                   TestEquality((BlockStatement) a, (BlockStatement) b);
        }

        private static bool TestEquality(IfStatement a, IfStatement b) {
            return OrderedCollectionsAreEqual(a.ElseStatements,b.ElseStatements,StatementsAreEqual) &&
                   TestEquality((ConditionBlockStatement) a, (ConditionBlockStatement) b);
        }

        private static bool TestEquality(CaseStatement a, CaseStatement b) {
            return a.IsDefault == b.IsDefault &&
                   TestEquality((ConditionBlockStatement) a, (ConditionBlockStatement) b);
        }

        private static bool TestEquality(ForStatement a, ForStatement b) {
            return ExpressionsAreEqual(a.Initializer, b.Initializer) &&
                   ExpressionsAreEqual(a.Incrementer, b.Incrementer) &&
                   TestEquality((ConditionBlockStatement)a, (ConditionBlockStatement)b);
        }

        #endregion Statement equality methods

        #region Expression equality methods
        private static bool TestEquality(Expression a, Expression b) {
            return a.ProgrammingLanguage == b.ProgrammingLanguage &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   OrderedCollectionsAreEqual(a.Components, b.Components, ExpressionsAreEqual);
        }

        private static bool TestEquality(OperatorUse a, OperatorUse b) {
            return a.Text == b.Text &&
                   TestEquality((Expression)a, (Expression)b);
        }

        private static bool TestEquality(VariableDeclaration a, VariableDeclaration b) {
            return a.Name == b.Name &&
                   ExpressionsAreEqual(a.Initializer, b.Initializer) &&
                   ExpressionsAreEqual(a.VariableType, b.VariableType) &&
                   TestEquality((Expression)a, (Expression)b);
            //Accessibility is not compared because this field is not merged/un-merged losslessly
        }

        private static bool TestEquality(LiteralUse a, LiteralUse b) {
            return a.Kind == b.Kind &&
                   a.Value == b.Value &&
                   TestEquality((Expression)a, (Expression)b);
        }

        private static bool TestEquality(NameUse a, NameUse b) {
            return a.Name == b.Name &&
                   ExpressionsAreEqual(a.Prefix, b.Prefix) &&
                   TestEquality((Expression)a, (Expression)b);
        }

        private static bool TestEquality(TypeUse a, TypeUse b) {
            return OrderedCollectionsAreEqual(a.TypeParameters, b.TypeParameters, ExpressionsAreEqual) &&
                   TestEquality((NameUse)a, (NameUse)b);
        }

        private static bool TestEquality(MethodCall a, MethodCall b) {
            return a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   OrderedCollectionsAreEqual(a.Arguments, b.Arguments, ExpressionsAreEqual) &&
                   OrderedCollectionsAreEqual(a.TypeArguments, b.TypeArguments, ExpressionsAreEqual) &&
                   TestEquality((NameUse)a, (NameUse)b);
        }

        private static bool TestEquality(VariableUse a, VariableUse b) {
            return ExpressionsAreEqual(a.Index, b.Index) &&
                   TestEquality((NameUse)a, (NameUse)b);
        }

        #endregion Expression equality methods
    }
}