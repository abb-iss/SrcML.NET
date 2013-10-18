using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Linq;

namespace ABB.SrcML.Data {

    public interface IScope : IRootedObject {

        IEnumerable<IScope> ChildScopes { get; }

        IEnumerable<IVariableDeclaration> DeclaredVariables { get; }

        IEnumerable<SrcMLLocation> DefinitionLocations { get; }

        string Id { get; }

        IEnumerable<SrcMLLocation> Locations { get; }

        IEnumerable<IMethodCall> MethodCalls { get; }

        SrcMLLocation PrimaryLocation { get; }

        Language ProgrammingLanguage { get; set; }

        IEnumerable<SrcMLLocation> ReferenceLocations { get; }

        void AddChildScope(IScope childScope);

        void AddDeclaredVariable(IVariableDeclaration declaration);

        IScope AddFrom(IScope otherScope);

        void AddMethodCall(IMethodCall methodCall);

        void AddSourceLocation(SrcMLLocation location);

        bool CanBeMergedInto(IScope otherScope);

        bool ExistsInFile(string fileName);

        IEnumerable<T> GetChildScopes<T>() where T : IScope;

        IEnumerable<IScope> GetChildScopesWithId(string id);

        IEnumerable<T> GetChildScopesWithId<T>(string id) where T : IScope;

        IEnumerable<IVariableDeclaration> GetDeclarationsForVariableName(string variableName, string xpath);

        IEnumerable<IScope> GetDescendantScopes();

        IEnumerable<T> GetDescendantScopes<T>() where T : IScope;

        IEnumerable<IScope> GetDescendantScopesAndSelf();

        IEnumerable<T> GetDescendantScopesAndSelf<T>() where T : IScope;

        T GetFirstDescendant<T>() where T : IScope;

        T GetFirstParent<T>() where T : IScope;

        System.Collections.ObjectModel.Collection<SrcMLLocation> GetLocationsInFile(string fileName);

        IEnumerable<IScope> GetParentScopes();

        IEnumerable<T> GetParentScopes<T>() where T : IScope;

        IEnumerable<IScope> GetParentScopesAndSelf();

        IEnumerable<T> GetParentScopesAndSelf<T>() where T : IScope;

        IScope GetScopeForLocation(SourceLocation loc);

        IScope GetScopeForLocation(string xpath);

        bool IsScopeFor(SourceLocation loc);

        bool IsScopeFor(string xpath);

        bool IsScopeFor(XElement element);

        IScope Merge(IScope otherScope);

        void RemoveChild(IScope childScope);

        Collection<IScope> RemoveFile(string fileName);
    }
}