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
        protected Dictionary<string, Collection<SrcMLLocation>> LocationDictionary;

        /// <summary>
        /// Initializes an empty variable scope.
        /// </summary>
        public Scope() {
            DeclaredVariablesDictionary = new Dictionary<string, VariableDeclaration>();
            ChildScopeCollection = new Collection<Scope>();
            MethodCallCollection = new Collection<MethodCall>();
            LocationDictionary = new Dictionary<string, Collection<SrcMLLocation>>();
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
            LocationDictionary = new Dictionary<string, Collection<SrcMLLocation>>(otherScope.LocationDictionary.Count);

            CopyFromOtherScope(otherScope);
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
        /// For Scope objects, the primary location is simply the first <see cref="SrcMLLocation.IsReference">non-reference</see>location that was added.
        /// if there are no <see cref="SrcMLLocation.IsReference">non-reference locations</see>, the first location is added.
        /// </summary>
        public virtual SrcMLLocation PrimaryLocation {
            get {
                if(DefinitionLocations.Any())
                    return DefinitionLocations.First();
                return ReferenceLocations.FirstOrDefault();
            }
        }

        /// <summary>
        /// An enumerable of all the source location objects that this scope is defined at.
        /// </summary>
        public IEnumerable<SrcMLLocation> Locations {
            get {
                var locations = from locationsForFile in LocationDictionary.Values
                                from location in locationsForFile
                                select location;
                return locations;
            }
        }

        /// <summary>
        /// An enumerable of all the locations where <see cref="SrcMLLocation.IsReference"/> is false
        /// </summary>
        public IEnumerable<SrcMLLocation> DefinitionLocations { get { return Locations.Where(l => !l.IsReference); } }

        /// <summary>
        /// An enumerable of all the locations where <see cref="SrcMLLocation.IsReference"/> is true
        /// </summary>
        public IEnumerable<SrcMLLocation> ReferenceLocations { get { return Locations.Where(l => l.IsReference); } }

        /// <summary>
        /// Gets all of the scopes from <see cref="ChildScopes"/> that match <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter child scopes with</typeparam>
        /// <returns>An enumerable of child scopes of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetChildScopes<T>() where T : Scope {
            return GetScopesOfType<T>(this.ChildScopes);
        }

        /// <summary>
        /// Gets all of the descendants from this scope. This is every scope that is rooted at this scope.
        /// </summary>
        /// <returns>The descendants of this scope</returns>
        public IEnumerable<Scope> GetDescendantScopes() {
            return GetDescendants(this, false);
        }

        /// <summary>
        /// Gets all of the descendants from this scope where the type is <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type to filter the descendant scopes by</typeparam>
        /// <returns>An enumerable of descendants of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetDescendantScopes<T>() where T : Scope {
            return GetScopesOfType<T>(GetDescendants(this, false));
        }

        /// <summary>
        /// Gets all of the descendants from this scope as well as the scope itself.
        /// </summary>
        /// <returns>This scope, followed by all of it descendants</returns>
        public IEnumerable<Scope> GetDescendantScopesAndSelf() {
            return GetDescendants(this, true);
        }

        /// <summary>
        /// Gets all of the scopes of type <typeparamref name="T"/> from the set of this scope and its descendants.
        /// </summary>
        /// <typeparam name="T">the type to filter by</typeparam>
        /// <returns>An enumerable of scopes of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetDescendantScopesAndSelf<T>() where T : Scope {
            return GetScopesOfType<T>(GetDescendants(this, true));
        }

        /// <summary>
        /// Gets the first descendant of this scope of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">the tyep to filter by</typeparam>
        /// <returns>the first matching descendant of this scope</returns>
        public T GetFirstDescendant<T>() where T : Scope {
            return GetDescendantScopes<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets the first scope of type <typeparamref name="T"/> from <see cref="GetParentScopesAndSelf{T}()"/>
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <returns>The first scope of type <typeparamref name="T"/></returns>
        public T GetFirstParent<T>() where T : Scope {
            return GetParentScopes<T>().FirstOrDefault();
        }

        /// <summary>
        /// Gets all of the parent scopes of this scope
        /// </summary>
        /// <returns>An enumerable (in reverse order) of all of the parent scopes</returns>
        public IEnumerable<Scope> GetParentScopes() {
            return GetParentsAndStartingPoint(this.ParentScope);
        }

        /// <summary>
        /// Gets all of the parent scopes of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <returns>An enumerable (in reverse order) of all of the parent scopes of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetParentScopes<T>() where T : Scope {
            return GetScopesOfType<T>(GetParentsAndStartingPoint(this.ParentScope));
        }

        /// <summary>
        /// returns an enumerable consisting of this element and all of its parents
        /// </summary>
        /// <returns>An enumerable (in reverse order) of this element and all of its parents</returns>
        public IEnumerable<Scope> GetParentScopesAndSelf() {
            return GetParentsAndStartingPoint(this);
        }

        /// <summary>
        /// Returns an enumerable consisting of this element and all of its parent scopes of type <typeparamref name="T"/>
        /// </summary>
        /// <typeparam name="T">The type to look for</typeparam>
        /// <returns>An enumerable (in reverse order) of this element and all of its parents of type <typeparamref name="T"/></returns>
        public IEnumerable<T> GetParentScopesAndSelf<T>() where T : Scope {
            return GetScopesOfType<T>(GetParentsAndStartingPoint(this));
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
        /// Adds a new srcML location to this scope.
        /// </summary>
        /// <param name="location">the location to add</param>
        public virtual void AddSourceLocation(SrcMLLocation location) {
            Collection<SrcMLLocation> locationsForFile;
            if(!LocationDictionary.TryGetValue(location.SourceFileName, out locationsForFile)) {
                locationsForFile = new Collection<SrcMLLocation>();
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
        public Collection<SrcMLLocation> GetLocationsInFile(string fileName) {
            Collection<SrcMLLocation> locations;
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
        public virtual Scope AddFrom(Scope otherScope) {
            CopyFromOtherScope(otherScope);
            return this;
        }

        /// <summary>
        /// Tests value equality between this scope and <paramref name="otherScope"/>.
        /// Two scopes are equal if they have the same <see cref="SrcMLLocation.XPath"/>.
        /// </summary>
        /// <param name="otherScope">The scope to compare to</param>
        /// <returns>True if the scopes are the same. False otherwise.</returns>
        public virtual bool CanBeMergedInto(Scope otherScope) {
            return (null != otherScope && this.PrimaryLocation.XPath == otherScope.PrimaryLocation.XPath);
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XElement. A variable scope contains an element if <see cref="SrcMLLocation.XPath"/> is a 
        /// prefix for the XPath for <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to look for</param>
        /// <returns>true if this is a container for <paramref name="element"/>. False otherwise.</returns>
        public bool IsScopeFor(XElement element) {
            return IsScopeFor(element.GetXPath(false));
        }

        /// <summary>
        /// Returns true if this variable scope contains the given XPath. A variable scope contains an xpath if <see cref="SrcMLLocation.XPath"/> is a prefix for <paramref name="xpath"/>
        /// </summary>
        /// <param name="xpath">The xpath to look for.</param>
        /// <returns>True if this is a container for the given xpath. False, otherwise.</returns>
        public virtual bool IsScopeFor(string xpath) {
            return Locations.Any(l => xpath.StartsWith(l.XPath));
        }

        /// <summary>
        /// Returns true if this scope surrounds the given source location. 
        /// </summary>
        /// <param name="loc">The source location to look for.</param>
        /// <returns>True if this is a container for the given location, False otherwise.</returns>
        public virtual bool IsScopeFor(SourceLocation loc) {
            return Locations.Any(l => l.Contains(loc));
        }

        /// <summary>
        /// Returns the innermost scope that contains the given xpath. 
        /// </summary>
        /// <param name="xpath">the xpath to find containers for.</param>
        /// <returns>The lowest child of this scope that contains the given xpath, or null if it cannot be found.</returns>
        public Scope GetScopeForLocation(string xpath) {
            //first search in children
            var foundScope = ChildScopeCollection.Select(c => c.GetScopeForLocation(xpath)).FirstOrDefault(r => r != null);
            //if xpath not found, check ourselves
            if(foundScope == null && this.IsScopeFor(xpath)) {
                foundScope = this;
            }
            return foundScope;
        }

        /// <summary>
        /// Returns the innermost scope that surrounds the given source location. 
        /// </summary>
        /// <param name="loc">The source location to search for.</param>
        /// <returns>The lowest child of this scope that surrounds the given location, or null if it cannot be found.</returns>
        public Scope GetScopeForLocation(SourceLocation loc) {
            //first search in children
            var foundScope = ChildScopeCollection.Select(c => c.GetScopeForLocation(loc)).FirstOrDefault(r => r != null);
            //if loc not found, check ourselves
            if(foundScope == null && this.IsScopeFor(loc)) {
                foundScope = this;
            }
            return foundScope;
        }

        /// <summary>
        /// Searches this scope and all of its children for variable declarations that match the given variable name and xpath.
        /// It finds all the scopes where the xpath is valid and all of the declarations in those scopes that match the variable name.
        /// </summary>
        /// <param name="variableName">the variable name to search for.</param>
        /// <param name="xpath">the xpath for the variable name</param>
        /// <returns>An enumerable of matching variable declarations.</returns>
        public IEnumerable<VariableDeclaration> GetDeclarationsForVariableName(string variableName, string xpath) {
            var lowestScope = GetScopeForLocation(xpath);
            foreach(var scope in lowestScope.GetParentScopesAndSelf()) {
                VariableDeclaration declaration;
                if(scope.DeclaredVariablesDictionary.TryGetValue(variableName, out declaration)) {
                    yield return declaration;
                }
            }
        }

        /// <summary>
        /// Removes the given child scope.
        /// </summary>
        /// <param name="childScope">The child scope to remove.</param>
        public virtual void RemoveChild(Scope childScope) {
            //remove child
            ChildScopeCollection.Remove(childScope);
            //update locations
            foreach(var childKvp in childScope.LocationDictionary) {
                var fileName = childKvp.Key;
                if(LocationDictionary.ContainsKey(fileName)) {
                    var fileLocations = LocationDictionary[fileName];
                    foreach(var childLoc in childKvp.Value) {
                        fileLocations.Remove(childLoc);
                    }
                    if(fileLocations.Count == 0) {
                        LocationDictionary.Remove(fileName);
                    }
                }
            }
        }

        /// <summary>
        /// Removes any program elements defined in the given file.
        /// If the scope is defined entirely within the given file, then it removes itself from its parent.
        /// </summary>
        /// <param name="fileName">The file to remove.</param>
        /// <returns>A collection of any unresolved scopes that result from removing the file. The caller is responsible for re-resolving these as appropriate.</returns>
        public virtual Collection<Scope> RemoveFile(string fileName) {
            if(LocationDictionary.ContainsKey(fileName)) {
                if(LocationDictionary.Count == 1) {
                    //this scope exists solely in the file to be deleted
                    if(ParentScope != null) {
                        ParentScope.RemoveChild(this);
                        ParentScope = null;
                    }
                } else {
                    Debug.WriteLine("Found Scope with more than one location. Should this be possible?");
                    foreach(var loc in Locations) {
                        Debug.WriteLine("Location: " + loc);
                    }

                    //Remove the file from the children
                    var unresolvedChildScopes = new List<Scope>();
                    foreach(var child in ChildScopeCollection.ToList()) {
                        var result = child.RemoveFile(fileName);
                        if(result != null) {
                            unresolvedChildScopes.AddRange(result);
                        }
                    }
                    if(unresolvedChildScopes.Count > 0) {
                        foreach(var child in unresolvedChildScopes) {
                            AddChildScope(child);
                        }
                    }
                    //remove method calls
                    var callsInFile = MethodCallCollection.Where(call => call.Location.SourceFileName == fileName).ToList();
                    foreach(var call in callsInFile) {
                        MethodCallCollection.Remove(call);
                    }
                    //remove declared variables
                    var declsInFile = DeclaredVariablesDictionary.Where(kvp => kvp.Value.Location.SourceFileName == fileName).ToList();
                    foreach(var kvp in declsInFile) {
                        DeclaredVariablesDictionary.Remove(kvp.Key);
                    }
                    //update locations
                    LocationDictionary.Remove(fileName);
                }
            }
            return null;
        }

        /// <summary>
        /// Copies the values from another Scope into this one.
        /// This is intentially separate from AddFrom(), because it is called from the copy constructor and so must be non-virtual.
        /// </summary>
        /// <param name="otherScope"></param>
        private void CopyFromOtherScope(Scope otherScope) {
            if(otherScope == null) { return; }

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
        }

        private static IEnumerable<Scope> GetDescendants(Scope startingPoint, bool returnSelf) {
            if(returnSelf) {
                yield return startingPoint;
            }

            foreach(var scope in startingPoint.ChildScopes) {
                foreach(var descendant in GetDescendants(scope, true)) {
                    yield return descendant;
                }
            }
        }

        private static IEnumerable<Scope> GetParentsAndStartingPoint(Scope startingPoint) {
            var current = startingPoint;
            while(current != null) {
                yield return current;
                current = current.ParentScope;
            }
        }

        private static IEnumerable<T> GetScopesOfType<T>(IEnumerable<Scope> scopes) where T : Scope {
            var results = from scope in scopes
                          let scopeAsT = scope as T
                          where scopeAsT != null
                          select scopeAsT;
            return results;
        }
    }
}
