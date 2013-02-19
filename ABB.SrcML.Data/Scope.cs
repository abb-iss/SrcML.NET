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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ABB.SrcML.Data {
    /// <summary>
    /// The Scope class is the base class for variable scope objects. It encapsulates the basics of the type hierarchy (parent-child relationships)
    /// and contains methods for adding child scopes and variable declarations.
    /// </summary>
    public class Scope {
        /// <summary>
        /// Holds all of the children for this scope.
        /// </summary>
        protected Collection<Scope> ChildScopeCollection;

        /// <summary>
        /// Holds all of the variable declarations declared here. The key is the variable name.
        /// </summary>
        protected Dictionary<string, VariableDeclaration> DeclaredVariablesDictionary;

        /// <summary>
        /// Holds all of the method calls for this scope
        /// </summary>
        protected Collection<MethodCall> MethodCallCollection;

        /// <summary>
        /// Holds all of the source locations for this scope. The key is a filename.
        /// the value is a collection of sourcelocations in that file where this scope has been defined.
        /// </summary>
        protected Dictionary<string, Collection<SourceLocation>> LocationDictionary;

        /// <summary>
        /// Initializes an empty variable scope.
        /// </summary>
        public Scope() {
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>();
            ChildScopeCollection = new Collection<Scope>();
            MethodCallCollection = new Collection<MethodCall>();
            LocationDictionary = new Dictionary<string, Collection<SourceLocation>>();
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="otherScope">The scope to copy from</param>
        public Scope(Scope otherScope) {
            ProgrammingLanguage = otherScope.ProgrammingLanguage;

            ChildScopeCollection = new Collection<Scope>();
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>(otherScope.DeclaredVariablesDictionary.Count);
            MethodCallCollection = new Collection<MethodCall>();
            LocationDictionary = new Dictionary<string, Collection<SourceLocation>>(otherScope.LocationDictionary.Count);

            AddFrom(otherScope);
        }

        /// <summary>
        /// Indicates the programming language used to create this scope
        /// </summary>
        public Language ProgrammingLanguage { get; set; }

        /// <summary>
        /// The parent container for this scope.
        /// </summary>
        public Scope ParentScope { get; set; }

        /// <summary>
        /// Iterates over all of the child scopes of this scope
        /// </summary>
        public IEnumerable<Scope> ChildScopes { get { return this.ChildScopeCollection.AsEnumerable(); } }

        /// <summary>
        /// Iterates over all of the variable declarations for this scope
        /// </summary>
        public IEnumerable<VariableDeclaration> DeclaredVariables { get { return this.DeclaredVariablesDictionary.Values.AsEnumerable(); } }

        /// <summary>
        /// Iterates over all of the method calls in this scope
        /// </summary>
        public IEnumerable<MethodCall> MethodCalls { get { return this.MethodCallCollection.AsEnumerable(); } }

        /// <summary>
        /// References the primary location where this location has been defined.
        /// For Scope objects, the primary location is simply the first <see cref="SourceLocation.IsReference">non-reference</see>location that was added.
        /// if there are no <see cref="SourceLocation.IsReference">non-reference locations</see>, the first location is added.
        /// </summary>
        public virtual SourceLocation PrimaryLocation {
            get {
                if(DefinitionLocations.Any())
                    return DefinitionLocations.First();
                return ReferenceLocations.FirstOrDefault();
            }
        }

        /// <summary>
        /// An enumerable of all the source location objects that this scope is defined at.
        /// </summary>
        public IEnumerable<SourceLocation> Locations {
            get {
                var locations = from locationsForFile in LocationDictionary.Values
                                from location in locationsForFile
                                select location;
                return locations;
            }
        }

        /// <summary>
        /// An enumerable of all the locations where <see cref="SourceLocation.IsReference"/> is false
        /// </summary>
        public IEnumerable<SourceLocation> DefinitionLocations { get { return Locations.Where(l => !l.IsReference); } }

        /// <summary>
        /// An enumerable of all the locations where <see cref="SourceLocation.IsReference"/> is true
        /// </summary>
        public IEnumerable<SourceLocation> ReferenceLocations { get { return Locations.Where(l => l.IsReference); } }

        /// <summary>
        /// The parent scopes for this scope in reverse order (parent is returned first, followed by the grandparent, etc).
        /// </summary>
        public IEnumerable<Scope> ParentScopes {
            get {
                var current = this.ParentScope;
                while(null != current) {
                    yield return current;
                    current = current.ParentScope;
                }
            }
        }

        /// <summary>
        /// The namespace name for this scope. The namespace name is taken by iterating over all of the parents and selecting only the namespace definitions.
        /// </summary>
        public string NamespaceName {
            get {
                var namespaceParents = from p in this.ParentScopes
                                       let ns = (p as NamespaceDefinition)
                                       where !(null == ns || ns.IsGlobal)
                                       select ns.Name;
                return String.Join(".", namespaceParents.Reverse());
            }
        }

        /// <summary>
        /// Adds a child scope to this scope
        /// </summary>
        /// <param name="childScope">The child scope to add.</param>
        public virtual void AddChildScope(Scope childScope) {
            int i;
            Scope mergedScope = null;
            
            for(i = 0; i < this.ChildScopeCollection.Count; i++) {
                mergedScope = this.ChildScopeCollection.ElementAt(i).Merge(childScope);
                if(null != mergedScope) {
                    this.ChildScopeCollection[i] = mergedScope;
                    mergedScope.ParentScope = this;
                    break;
                }
            }

            if(null == mergedScope) {
                ChildScopeCollection.Add(childScope);
                childScope.ParentScope = this;
            }
        }

        /// <summary>
        /// Add a variable declaration to this scope
        /// </summary>
        /// <param name="declaration">The variable declaration to add.</param>
        public void AddDeclaredVariable(VariableDeclaration declaration) {
            DeclaredVariablesDictionary[declaration.Name] = declaration;
            declaration.Scope = this;
        }

        /// <summary>
        /// Adds a method call
        /// </summary>
        /// <param name="methodCall">the method call to add</param>
        public void AddMethodCall(MethodCall methodCall) {
            MethodCallCollection.Add(methodCall);
            methodCall.ParentScope = this;
        }

        /// <summary>
        /// Adds a new source location to this scope.
        /// </summary>
        /// <param name="location">the location to add</param>
        public virtual void AddSourceLocation(SourceLocation location) {
            Collection<SourceLocation> locationsForFile;
            if(!LocationDictionary.TryGetValue(location.SourceFileName, out locationsForFile)) {
                locationsForFile = new Collection<SourceLocation>();
                LocationDictionary[location.SourceFileName] = locationsForFile;
            }
            locationsForFile.Add(location);
        }

        /// <summary>
        /// Checks if this scope was defined in <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The filename to lookup.</param>
        /// <returns>True if this scope contains locations in <paramref name="fileName"/>; false otherwise</returns>
        public bool ExistsInFile(string fileName) {
            return LocationDictionary.ContainsKey(fileName);
        }

        /// <summary>
        /// Gets all of the locations for a particular <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The file name to get locations for</param>
        /// <returns>A collection of <see cref="SourceLocation"/> objects. If this scope was not defined in <paramref name="fileName"/>, null is returned.</returns>
        public Collection<SourceLocation> GetLocationsInFile(string fileName) {
            Collection<SourceLocation> locations;
            if(LocationDictionary.TryGetValue(fileName, out locations)) {
                return locations;
            }
            return null;
        }

        /// <summary>
        /// The merge function merges two scopes if they are the same. It assumes that the parents of the two scopes are identical.
        /// Because of this, it is best to call it on two "global scope" objects. If the two scopes are the same (as determined by
        /// the <see cref="CanBeMergedInto"/> method), then the variable declarations in <paramref name="otherScope"/> then a new
        /// Scope with all the children of the both scopes is returned.
        /// </summary>
        /// <param name="otherScope">The scope to merge with</param>
        /// <returns>A new variable scope if the scopes <see cref="CanBeMergedInto">could be merged</see>; null otherwise</returns>
        public virtual Scope Merge(Scope otherScope) {
            if(CanBeMergedInto(otherScope)) {
                Scope mergedScope = new Scope(this);
                mergedScope.AddFrom(otherScope);
                return mergedScope;
            }
            return null;
        }

        /// <summary>
        /// The AddFrom function adds all of the declarations and children from <paramref name="otherScope"/> to this scope
        /// </summary>
        /// <param name="otherScope">The scope to add data from</param>
        /// <returns>the new scope</returns>
        public Scope AddFrom(Scope otherScope) {
            foreach(var declaration in otherScope.DeclaredVariables) {
                this.AddDeclaredVariable(declaration);
            }
            foreach(var location in otherScope.Locations) {
                this.AddSourceLocation(location);
            }
            foreach(var newChild in otherScope.ChildScopes) {
                AddChildScope(newChild);
            }
            foreach(var methodCall in otherScope.MethodCalls) {
                AddMethodCall(methodCall);
            }
            return this;
        }

        /// <summary>
        /// Tests value equality between this scope and <paramref name="otherScope"/>.
        /// Two scopes are equal if they have the same <see cref="SourceLocation.XPath"/>.
        /// </summary>
        /// <param name="otherScope">The scope to compare to</param>
        /// <returns>True if the scopes are the same. False otherwise.</returns>
        public virtual bool CanBeMergedInto(Scope otherScope) {
            return (null != otherScope && this.PrimaryLocation.XPath == otherScope.PrimaryLocation.XPath);
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XElement. A variable scope contains an element if <see cref="SourceLocation.XPath"/> is a 
        /// prefix for the XPath for <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to look for</param>
        /// <returns>true if this is a container for <paramref name="element"/>. False otherwise.</returns>
        public bool IsScopeFor(XElement element) {
            return IsScopeFor(element.GetXPath(false));
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XPath. A variable scope contains an expath if <see cref="SourceLocation.XPath"/> is a prefix for <paramref name="xpath"/>
        /// </summary>
        /// <param name="xpath">The xpath to look for.</param>
        /// <returns>True if this is a container for the given xpath. False, otherwise.</returns>
        public virtual bool IsScopeFor(string xpath) {
            return xpath.StartsWith(this.PrimaryLocation.XPath);
        }

        /// <summary>
        /// returns an enumerable of all the scopes rooted here that are containers for this XPath. Includes the current scope.
        /// </summary>
        /// <param name="xpath">the xpath to find containers for.</param>
        /// <returns>an enumerable of all the scopes rooted here that are containers for this XPath. Includes the current scope.</returns>
        public IEnumerable<Scope> GetScopesForPath(string xpath) {
            if(IsScopeFor(xpath)) {
                yield return this;

                foreach(var child in this.ChildScopes) {
                    foreach(var matchingScope in child.GetScopesForPath(xpath)) {
                        yield return matchingScope;
                    }
                }
            }
        }

        /// <summary>
        /// Searches this scope and all of its children for variable declarations that match the given variable name and xpath.
        /// It finds all the scopes where the xpath is valid and all of the declarations in those scopes that match the variable name.
        /// </summary>
        /// <param name="variableName">the variable name to search for.</param>
        /// <param name="xpath">the xpath for the variable name</param>
        /// <returns>An enumerable of matching variable declarations.</returns>
        public IEnumerable<VariableDeclaration> GetDeclarationsForVariableName(string variableName, string xpath) {
            foreach(var scope in GetScopesForPath(xpath)) {
                VariableDeclaration declaration;
                if(scope.DeclaredVariablesDictionary.TryGetValue(variableName, out declaration)) {
                    yield return declaration;
                }
            }
        }

        /// <summary>
        /// Removes any program elements defined in the given file.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        public virtual void RemoveFile(string fileName) {
            if(!LocationDictionary.ContainsKey(fileName)) {
                //this scope is not defined in the given file
                return;
            }

            if(LocationDictionary.Count == 1) {
                //this scope exists solely in the file to be deleted
                ParentScope = null;
            } else {
                Debug.WriteLine("Found Scope with more than one location. This should be impossible!");
                foreach(var loc in Locations) {
                    Debug.WriteLine("Location: " + loc);
                }
            }
        }
    }
}
