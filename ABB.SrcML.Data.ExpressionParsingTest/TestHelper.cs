using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ABB.SrcML.Data.Test {

    public static class TestHelper {

        public static bool AliasesAreEqual(Alias a, Alias b) {
            if(a == b) { return true; }
            return LocationsAreEqual(a.Location, b.Location) &&
                   NamedScopeUsesAreEqual(a.ImportedNamedScope, b.ImportedNamedScope) &&
                   NamedScopeUsesAreEqual(a.ImportedNamespace, b.ImportedNamespace);
        }

        public static bool IResolvesToTypesAreEqual(IResolvesToType a, IResolvesToType b) {
            throw new NotImplementedException();

            //TODO: reimplement this using proper OO-ish design
            //if(a == b) { return true; }
            //if(a == null || b == null) { return false; }
            //var aType = a.GetType();
            //if(aType != b.GetType()) { return false; }
            //if(aType.Name == "VariableUse") {
            //    return VariableUsesAreEqual((VariableUse) a, (VariableUse) b);
            //} else if(aType.Name == "MethodCall") {
            //    return MethodCallsAreEqual(MethodCall) a, (MethodCall) b);
            //} else if(aType.Name == "TypeUse") {
            //    return TypeUsesAreEqual((TypeUse) a, (TypeUse) b);
            //} else if(aType.Name == "LiteralUse") {
            //    return LiteralUsesAreEqual((LiteralUse) a, (LiteralUse) b);
            //}

            //return false;
        }

        public static bool ExpressionsAreEqual(Expression a, Expression b) {
            //TODO: implement ExpressionsAreEqual properly
            
            return true;
        }

        public static bool LiteralUsesAreEqual(LiteralUse a, LiteralUse b) {
            if(a == b) { return true; }
            return a.Kind == b.Kind &&
                   TypeUsesAreEqual(a, b);
        }

        public static bool LocationsAreEqual(SrcMLLocation a, SrcMLLocation b) {
            if(a == b) { return true; }
            return a.IsReference == b.IsReference &&
                   a.StartingColumnNumber == b.StartingColumnNumber &&
                   a.SourceFileName == b.SourceFileName &&
                   a.StartingLineNumber == b.StartingLineNumber &&
                   a.XPath == b.XPath;
        }

        public static bool MethodCallsAreEqual(MethodCall a, MethodCall b) {
            if(a == b) { return true; }
            return OrderedCollectionsAreEqual(a.Arguments, b.Arguments, ExpressionsAreEqual) &&
                   IResolvesToTypesAreEqual(a.CallingObject, b.CallingObject) &&
                   a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name;
        }

        public static bool NameUsesAreEqual(NameUse a, NameUse b) {
            if(a == b) { return true; }

            return a.Name == b.Name && NamePrefixesAreEqual(a.Prefix, b.Prefix);
        }
        public static bool NamePrefixesAreEqual(NamePrefix a, NamePrefix b) {
            if(a == b) { return true; }
            return OrderedCollectionsAreEqual(a.Names.ToList(), b.Names.ToList(), NameUsesAreEqual);
        }
        public static bool NamedScopeUsesAreEqual(NamedScopeUse a, NamedScopeUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.ProgrammingLanguage == b.ProgrammingLanguage &&
                   NamedScopeUsesAreEqual(a.ChildScopeUse, b.ChildScopeUse);
        }

        public static bool StatementsAreEqual(Statement a, Statement b) {
            if(a == b) { return true; }
            
            if(a.GetType() != b.GetType()) { return false; }
            return TestEquality((dynamic) a, (dynamic) b);
        }

        public static bool TypeUsesAreEqual(TypeUse a, TypeUse b) {
            if(a == b) { return true; }
            return LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   a.IsGeneric == b.IsGeneric &&
                   OrderedCollectionsAreEqual(a.TypeParameters, b.TypeParameters, TypeUsesAreEqual);
        }

        public static bool VariableDeclarationsAreEqual(VariableDeclaration a, VariableDeclaration b) {
            if(a == b) { return true; }
            return LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   // TODO a.Accessibility == b.Accessibility &&
                   TypeUsesAreEqual(a.VariableType, b.VariableType);
        }

        public static bool VariableUsesAreEqual(VariableUse a, VariableUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   IResolvesToTypesAreEqual(a.CallingObject, b.CallingObject);
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

        private static bool TestEquality(Statement a, Statement b) {
            return OrderedCollectionsAreEqual(a.ChildStatements.ToList(), b.ChildStatements.ToList(), StatementsAreEqual) &&
                   CollectionsAreEqual(a.Locations.ToList(), b.Locations.ToList(), LocationsAreEqual) &&
                   a.ProgrammingLanguage == b.ProgrammingLanguage;
        }

        private static bool TestEquality(NamedScope a, NamedScope b) {
            return a.Name == b.Name &&
                   NamePrefixesAreEqual(a.Prefix, b.Prefix) &&
                   TestEquality((Statement) a, (Statement) b);
        }

        private static bool TestEquality(NamespaceDefinition a, NamespaceDefinition b) {
            return a.IsAnonymous == b.IsAnonymous &&
                   TestEquality((NamedScope) a, (NamedScope) b);
        }

        private static bool TestEquality(TypeDefinition a, TypeDefinition b) {
            return a.IsPartial == b.IsPartial &&
                   a.Kind == b.Kind &&
                   CollectionsAreEqual(a.ParentTypes, b.ParentTypes, TypeUsesAreEqual) &&
                   TestEquality((NamedScope) a, (NamedScope) b);
        }

        private static bool TestEquality(MethodDefinition a, MethodDefinition b) {
            return a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   TypeUsesAreEqual(a.ReturnType, b.ReturnType) &&
                   OrderedCollectionsAreEqual(a.Parameters, b.Parameters, VariableDeclarationsAreEqual) &&
                   TestEquality((NamedScope) a, (NamedScope) b);
        }
    }
}