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
            //TODO: reimplement this using proper OO-ish design
            if(a == b) { return true; }
            if(a == null || b == null) { return false; }
            var aType = a.GetType();
            if(aType != b.GetType()) { return false; }
            if(aType.Name == "VariableUse") {
                return VariableUsesAreEqual((VariableUse) a, (VariableUse) b);
            } else if(aType.Name == "MethodCall") {
                return MethodCallsAreEqual((MethodCall) a, (MethodCall) b);
            } else if(aType.Name == "TypeUse") {
                return TypeUsesAreEqual((TypeUse) a, (TypeUse) b);
            } else if(aType.Name == "LiteralUse") {
                return LiteralUsesAreEqual((LiteralUse) a, (LiteralUse) b);
            }

            return false;
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
            return CollectionsAreEqual(a.Arguments, b.Arguments, IResolvesToTypesAreEqual) &&
                   IResolvesToTypesAreEqual(a.CallingObject, b.CallingObject) &&
                   a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name;
        }

        public static bool NamedScopeUsesAreEqual(NamedScopeUse a, NamedScopeUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.ProgrammingLanguage == b.ProgrammingLanguage &&
                   NamedScopeUsesAreEqual(a.ChildScopeUse, b.ChildScopeUse);
        }

        public static bool ParameterDeclarationsAreEqual(ParameterDeclaration a, ParameterDeclaration b) {
            if(a == b) { return true; }
            //we intentially don't test parameter names, because those may differ between signatures
            //we also intentionally ignore the parameter type locations
            var variableTypesEqual = a.VariableType.Name == b.VariableType.Name &&
                                     NamedScopeUsesAreEqual(a.VariableType.Prefix, b.VariableType.Prefix);
            return variableTypesEqual &&
                   CollectionsAreEqual(a.Locations, b.Locations, LocationsAreEqual);
        }

        public static bool ScopesAreEqual(IScope a, IScope b) {
            if(a == b) { return true; }
            //Assert.AreEqual(a.GetType(), b.GetType());
            if(a.GetType() != b.GetType()) { return false; }
            return TestEquality((dynamic) a, (dynamic) b);
        }

        public static bool TypeUsesAreEqual(TypeUse a, TypeUse b) {
            if(a == b) { return true; }
            return LocationsAreEqual(a.Location, b.Location) &&
                   NamedScopeUsesAreEqual(a.Prefix, b.Prefix) &&
                   a.Name == b.Name &&
                   IResolvesToTypesAreEqual(a.CallingObject, b.CallingObject);
        }

        public static bool VariableDeclarationsAreEqual(IVariableDeclaration a, IVariableDeclaration b) {
            if(a == b) { return true; }
            return a.Accessibility == b.Accessibility &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   TypeUsesAreEqual(a.VariableType, b.VariableType);
        }

        public static bool VariableUsesAreEqual(VariableUse a, VariableUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   IResolvesToTypesAreEqual(a.CallingObject, b.CallingObject);
        }

        public static void VerifyPrefixValues(IEnumerable<string> expected, NamedScopeUse use) {
            var prefix = use;
            Collection<string> prefixes = new Collection<string>();
            do {
                prefixes.Add(prefix.Name);
                prefix = prefix.ChildScopeUse;
            } while(prefix != null);

            CollectionAssert.AreEqual(expected as IEnumerable, prefixes as IEnumerable);
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

        private static bool TestEquality(IScope a, IScope b) {
            //Assert.IsTrue(CollectionsAreEqual(a.ChildScopes.ToList(), b.ChildScopes.ToList(), ScopesAreEqual));
            //Assert.IsTrue(CollectionsAreEqual(a.MethodCalls.ToList(), b.MethodCalls.ToList(), MethodCallsAreEqual));
            //Assert.IsTrue(CollectionsAreEqual(a.DeclaredVariables.ToList(), b.DeclaredVariables.ToList(), VariableDeclarationsAreEqual));
            //Assert.IsTrue(CollectionsAreEqual(a.Locations.ToList(), b.Locations.ToList(), LocationsAreEqual));
            //Assert.AreEqual(a.ProgrammingLanguage, b.ProgrammingLanguage);
            return CollectionsAreEqual(a.ChildScopes.ToList(), b.ChildScopes.ToList(), ScopesAreEqual) &&
                   CollectionsAreEqual(a.MethodCalls.ToList(), b.MethodCalls.ToList(), MethodCallsAreEqual) &&
                   CollectionsAreEqual(a.DeclaredVariables.ToList(), b.DeclaredVariables.ToList(), VariableDeclarationsAreEqual) &&
                   CollectionsAreEqual(a.Locations.ToList(), b.Locations.ToList(), LocationsAreEqual) &&
                   a.ProgrammingLanguage == b.ProgrammingLanguage;
        }

        private static bool TestEquality(INamedScope a, INamedScope b) {
            //Assert.AreEqual(a.Name, b.Name);
            ////Accessibility isn't undone right now, so don't check it
            ////Assert.AreEqual(a.Accessibility, b.Accessibility);
            //Assert.IsTrue(CollectionsAreEqual(a.ParentScopeCandidates, b.ParentScopeCandidates, NamedScopeUsesAreEqual));
            //Assert.IsTrue(NamedScopeUsesAreEqual(a.UnresolvedParentScopeInUse, b.UnresolvedParentScopeInUse));
            //return TestEquality((Scope)a, (Scope)b);
            return a.Name == b.Name &&
                   CollectionsAreEqual(a.ParentScopeCandidates, b.ParentScopeCandidates, NamedScopeUsesAreEqual) &&
                   NamedScopeUsesAreEqual(a.UnresolvedParentScopeInUse, b.UnresolvedParentScopeInUse) &&
                   TestEquality((Scope) a, (Scope) b);
        }

        private static bool TestEquality(NamespaceDefinition a, NamespaceDefinition b) {
            //Assert.AreEqual(a.IsAnonymous, b.IsAnonymous);
            //return TestEquality((NamedScope)a, (NamedScope)b);
            return a.IsAnonymous == b.IsAnonymous &&
                   TestEquality((INamedScope) a, (INamedScope) b);
        }

        private static bool TestEquality(TypeDefinition a, TypeDefinition b) {
            //Assert.AreEqual(a.IsPartial, b.IsPartial);
            //Assert.AreEqual(a.Kind, b.Kind);
            //Assert.IsTrue(CollectionsAreEqual(a.ParentTypes, b.ParentTypes, TypeUsesAreEqual));
            //return TestEquality((NamedScope)a, (NamedScope)b);
            return a.IsPartial == b.IsPartial &&
                   a.Kind == b.Kind &&
                   CollectionsAreEqual(a.ParentTypes, b.ParentTypes, TypeUsesAreEqual) &&
                   TestEquality((INamedScope) a, (INamedScope) b);
        }

        private static bool TestEquality(MethodDefinition a, MethodDefinition b) {
            //Assert.AreEqual(a.IsConstructor, b.IsConstructor);
            //Assert.AreEqual(a.IsDestructor, b.IsDestructor);
            //Assert.IsTrue(OrderedCollectionsAreEqual(a.Parameters, b.Parameters, ParameterDeclarationsAreEqual));
            //return TestEquality((NamedScope)a, (NamedScope)b);
            return a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   OrderedCollectionsAreEqual(a.Parameters, b.Parameters, ParameterDeclarationsAreEqual) &&
                   TestEquality((INamedScope) a, (INamedScope) b);
        }
    }
}