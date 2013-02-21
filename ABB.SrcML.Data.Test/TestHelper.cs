using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ABB.SrcML.Data.Test {
    public static class TestHelper {
        public static bool ScopesAreEqual(Scope a, Scope b) {
            if(a == b) { return true; }
            Assert.AreEqual(a.GetType(), b.GetType());
            return TestEquality((dynamic)a, (dynamic)b);
        }

        private static bool TestEquality(Scope a, Scope b) {
            Assert.IsTrue(CollectionsAreEqual(a.ChildScopes.ToList(), b.ChildScopes.ToList(), ScopesAreEqual));
            Assert.IsTrue(CollectionsAreEqual(a.MethodCalls.ToList(), b.MethodCalls.ToList(), MethodCallsAreEqual));
            Assert.IsTrue(CollectionsAreEqual(a.DeclaredVariables.ToList(), b.DeclaredVariables.ToList(), VariableDeclarationsAreEqual));
            Assert.IsTrue(CollectionsAreEqual(a.Locations.ToList(), b.Locations.ToList(), LocationsAreEqual));
            Assert.AreEqual(a.ProgrammingLanguage, b.ProgrammingLanguage);
            return true;
        }

        private static bool TestEquality(NamedScope a, NamedScope b) {
            Assert.AreEqual(a.Name, b.Name);
            Assert.AreEqual(a.Accessibility, b.Accessibility);
            Assert.IsTrue(CollectionsAreEqual(a.ParentScopeCandidates, b.ParentScopeCandidates, NamedScopeUsesAreEqual));
            Assert.IsTrue(NamedScopeUsesAreEqual(a.UnresolvedParentScopeInUse, b.UnresolvedParentScopeInUse));
            return TestEquality((Scope)a, (Scope)b);
        }

        private static bool TestEquality(NamespaceDefinition a, NamespaceDefinition b) {
            Assert.AreEqual(a.IsAnonymous, b.IsAnonymous);
            return TestEquality((NamedScope)a, (NamedScope)b);
        }

        private static bool TestEquality(TypeDefinition a, TypeDefinition b) {
            Assert.AreEqual(a.IsPartial, b.IsPartial);
            Assert.AreEqual(a.Kind, b.Kind);
            Assert.IsTrue(CollectionsAreEqual(a.ParentTypes, b.ParentTypes, TypeUsesAreEqual));
            return TestEquality((NamedScope)a, (NamedScope)b);
        }

        private static bool TestEquality(MethodDefinition a, MethodDefinition b) {
            Assert.AreEqual(a.IsConstructor, b.IsConstructor);
            Assert.AreEqual(a.IsDestructor, b.IsDestructor);
            Assert.IsTrue(CollectionsAreEqual(a.Parameters, b.Parameters, VariableDeclarationsAreEqual));
            return TestEquality((NamedScope)a, (NamedScope)b);
        }

        public static bool VariableDeclarationsAreEqual(VariableDeclaration a, VariableDeclaration b) {
            if(a == b) { return true; }
            return a.Accessibility == b.Accessibility &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   TypeUsesAreEqual(a.VariableType, b.VariableType);
        }

        public static bool TypeUsesAreEqual(TypeUse a, TypeUse b) {
            if(a == b) { return true; }
            bool aliasesEqual = CollectionsAreEqual(a.Aliases, b.Aliases, AliasesAreEqual);
            bool prefixesEqual = a.Prefix.Intersect(b.Prefix).Count() == a.Prefix.Count();
            return LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   prefixesEqual &&
                   aliasesEqual;
        }

        public static bool AliasesAreEqual(Alias a, Alias b) {
            if(a == b) { return true; }
            return LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name &&
                   a.NamespaceName == b.NamespaceName;
        }

        public static bool LocationsAreEqual(SourceLocation a, SourceLocation b) {
            if(a == b) { return true; }
            return a.IsReference == b.IsReference &&
                   a.SourceColumnNumber == b.SourceColumnNumber &&
                   a.SourceFileName == b.SourceFileName &&
                   a.SourceLineNumber == b.SourceLineNumber &&
                   a.XPath == b.XPath;
        }

        public static bool NamedScopeUsesAreEqual(NamedScopeUse a, NamedScopeUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.ProgrammingLanguage == b.ProgrammingLanguage &&
                   NamedScopeUsesAreEqual(a.ChildScopeUse, b.ChildScopeUse);
        }

        public static bool MethodCallsAreEqual(MethodCall a, MethodCall b) {
            if(a == b) { return true; }
            return CollectionsAreEqual(a.Arguments, b.Arguments, VariableUsesAreEqual) &&
                   VariableUsesAreEqual(a.Caller, b.Caller) &&
                   a.IsConstructor == b.IsConstructor &&
                   a.IsDestructor == b.IsDestructor &&
                   LocationsAreEqual(a.Location, b.Location) &&
                   a.Name == b.Name;
        }

        public static bool VariableUsesAreEqual(VariableUse a, VariableUse b) {
            if(a == b) { return true; }
            return a.Name == b.Name &&
                   LocationsAreEqual(a.Location, b.Location);
        }

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
    }
}
