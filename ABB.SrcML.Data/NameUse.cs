/******************************************************************************
 * Copyright (c) 2014 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Vinay Augustine (ABB Group) - initial API, implementation, & documentation
 *    Patrick Francis (ABB Group) - API, implementation, & documentation
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents the generalized use of a name. This does not distinguish whether the name represents a type, or variable, or what.
    /// </summary>
    public class NameUse : Expression {
        private NamePrefix prefix;
        /// <summary> The aliases and imports active at this use. </summary>
        private List<Statement> aliases;
        
        /// <summary> The XML name for NameUse </summary>
        public new const string XmlName = "n";

        /// <summary> XML Name for <see cref="Name" /> </summary>
        public const string XmlNameName = "val";

        /// <summary> XML Name for <see cref="Prefix" /> </summary>
        public const string XmlPrefixName = "Prefix";

        /// <summary>
        /// The binary operators that indicate that the name on the right-hand side is a child of the left-hand side.
        /// </summary>
        protected static readonly string[] NameInclusionOperators = {".", "->", "::"};

        /// <summary> The name being used. </summary>
        public string Name { get; set; }

        /// <summary>
        /// The prefix of the name. In a fully-qualified name like System.IO.File, the name is File and the prefix is System.IO.
        /// </summary>
        public NamePrefix Prefix {
            get { return prefix; }
            set {
                prefix = value;
                if(prefix != null) {
                    prefix.ParentExpression = this;
                    prefix.ParentStatement = this.ParentStatement;
                }
            }
        }

        /// <summary> The statement containing this expression. </summary>
        public override Statement ParentStatement {
            get { return base.ParentStatement; }
            set {
                base.ParentStatement = value;
                if(Prefix != null) { Prefix.ParentStatement = value; }
            }
        }

        /// <summary>
        /// Determines the set of aliases active at the site of this name use, sorted in reverse document order.
        /// </summary>
        /// <returns>The AliasStatements occuring prior to this NameUse.</returns>
        public IEnumerable<AliasStatement> GetAliases() {
            if(aliases == null) {
                aliases = DetermineAliases();
            }
            return aliases.OfType<AliasStatement>();
        }

        /// <summary>
        /// Determines the set of imports active at the site of this name use, sorted in reverse document order.
        /// </summary>
        /// <returns>The ImportStatements occuring prior to this NameUse.</returns>
        public IEnumerable<ImportStatement> GetImports() {
            if(aliases == null) {
                aliases = DetermineAliases();
            }
            return aliases.OfType<ImportStatement>();
        }
        

        /// <summary>
        /// Returns the child expressions, including the Prefix.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            if(Prefix != null) {
                return Enumerable.Repeat(Prefix, 1).Concat(base.GetChildren());
            } else {
                return base.GetChildren();
            }
        }

        /// <summary>
        /// Instance method for getting <see cref="NameUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for NameUse</returns>
        public override string GetXmlName() { return NameUse.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlPrefixName == reader.Name) {
                Prefix = XmlSerialization.ReadChildExpression(reader) as NamePrefix;
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Read the XML attributes from the current <paramref name="reader"/> position
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlAttributes(XmlReader reader) {
            string attribute = reader.GetAttribute(XmlNameName);
            if(!String.IsNullOrEmpty(attribute)) {
                Name = attribute;
            }
            base.ReadXmlAttributes(reader);
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Prefix) {
                XmlSerialization.WriteElement(writer, Prefix, XmlPrefixName);
            }
            base.WriteXmlContents(writer);
        }

        /// <summary>
        /// Writes XML attributes from this object to the XML writer
        /// </summary>
        /// <param name="writer">The XML writer</param>
        protected override void WriteXmlAttributes(XmlWriter writer) {
            writer.WriteAttributeString(XmlNameName, Name);
            base.WriteXmlAttributes(writer);
        }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            return string.Format("{0}{1}", Prefix, Name);
        }

        /// <summary>
        /// Finds definitions that match this name.
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<INamedEntity> FindMatches() {
            if(ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }

            //handle keywords
            if(Name == "this" ||
               (Name == "base" && ProgrammingLanguage == Language.CSharp) ||
               (Name == "super" && ProgrammingLanguage == Language.Java)) {
                return TypeDefinition.GetTypeForKeyword(this);
            }

            //We don't want to match a NameUse to a MethodDefinition, so exclude them in all the queries

            //If there's a prefix, resolve that and search under results
            if(Prefix != null) {
                return Prefix.FindMatches().SelectMany(ns => ns.GetNamedChildren<INamedEntity>(this.Name)).Where(e => !(e is MethodDefinition));
            }

            //If there's a calling expression, match and search under results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                IEnumerable<INamedEntity> matches = Enumerable.Empty<INamedEntity>();
                foreach(var scope in callingScopes) {
                    var localMatches = scope.GetNamedChildren(this.Name).Where(e => !(e is MethodDefinition)).ToList();
                    var callingType = scope as TypeDefinition;
                    if(!localMatches.Any() && callingType != null) {
                        //also search under the base types of the calling scope
                        matches = matches.Concat(callingType.SearchParentTypes<INamedEntity>(this.Name, e => !(e is MethodDefinition)));
                    } else {
                        matches = matches.Concat(localMatches);
                    }
                }
                return matches;
            }

            //search enclosing scopes and base types
            foreach(var scope in ParentStatement.GetAncestors()) {
                var matches = scope.GetNamedChildren(this).Where(e => !(e is MethodDefinition)).ToList();
                if(matches.Any()) {
                    return matches;
                }
                var expMatches = (from decl in scope.GetExpressions().SelectMany(e => e.GetDescendantsAndSelf<VariableDeclaration>())
                                  where decl.Name == this.Name
                                  select decl).ToList();
                if(expMatches.Any()) {
                    return expMatches;
                }
                var typeDef = scope as TypeDefinition;
                if(typeDef != null) {
                    var baseTypeMatches = typeDef.SearchParentTypes<INamedEntity>(this.Name, e => !(e is MethodDefinition)).ToList();
                    if(baseTypeMatches.Any()) {
                        return baseTypeMatches;
                    }
                }
            }

            //search if there is an alias for this name
            foreach(var alias in GetAliases()) {
                if(alias.AliasName == this.Name) {
                    var targetName = alias.Target as NameUse;
                    if(targetName == null) {
                        //Target is not a NameUse, probably an Expression
                        targetName = alias.Target.GetDescendantsAndSelf<NameUse>().LastOrDefault();
                    }
                    if(targetName != null) {
                        return targetName.FindMatches();
                    }
                }
            }

            //we didn't find it locally, search under imported namespaces
            return (from import in GetImports()
                    from match in import.ImportedNamespace.GetDescendantsAndSelf<NameUse>().Last().FindMatches().OfType<NamedScope>()
                    from child in match.GetNamedChildren(this.Name)
                    where !(child is MethodDefinition)
                    select child);
        }

        /// <summary>
        /// Determines the possible types of this expression.
        /// </summary>
        /// <returns>An enumerable of the matching TypeDefinitions for this expression's possible types.</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            //TODO: add type to INamedEntity interface, and update this method to retrieve it from the results of FindMatches()

            var matches = FindMatches();
            foreach(var match in matches) {
                if(match is TypeDefinition) {
                    yield return match as TypeDefinition;
                } else if(match is PropertyDefinition) {
                    foreach(var retType in ((PropertyDefinition)match).ReturnType.ResolveType()) {
                        yield return retType;
                    }
                } else if(match is MethodDefinition) {
                    foreach(var retType in ((MethodDefinition)match).ReturnType.ResolveType()) {
                        yield return retType;
                    }
                } else if(match is VariableDeclaration) {
                    foreach(var retType in ((VariableDeclaration)match).VariableType.ResolveType()) {
                        yield return retType;
                    }
                } 
            }
        }

        /// <summary>
        /// If there is a calling expession preceding this NameUse, this method resolves it
        /// to determine the scope(s) in which to search for the use's name.
        /// </summary>
        /// <returns>An enumerable of the named entities that may contain the name being used in this NameUse.
        /// Returns null if there is no suitable calling expression.
        /// Returns an empty enumerable if there is a calling expression, but no matches are found.</returns>
        protected IEnumerable<NamedScope> GetCallingScope() {
            var siblings = GetSiblingsBeforeSelf().ToList();
            var priorOp = siblings.LastOrDefault() as OperatorUse;
            if(priorOp == null || !NameInclusionOperators.Contains(priorOp.Text)) {
                return null;
            }

            if(siblings.Count == 1) {
                //This use is preceded by a name inclusion operator and nothing else
                //this is probably only possible in C++: ::MyGlobalClass
                //just return the global namespace
                return ParentStatement.GetAncestorsAndSelf<NamespaceDefinition>().Where(n => n.IsGlobal);
            }

            var callingExp = siblings[siblings.Count - 2]; //second-to-last sibling
            var callingName = callingExp as NameUse;
            if(callingName == null) {
                //Not a NameUse, probably an Expression
                return callingExp.ResolveType();
            }

            var matches = callingName.FindMatches();
            var scopes = new List<NamedScope>();
            foreach(var match in matches) {
                //TODO: update this to use polymorphism
                if(match is MethodDefinition) {
                    var method = match as MethodDefinition;
                    if(method.ReturnType != null) {
                        scopes.AddRange(((MethodDefinition)match).ReturnType.ResolveType());
                    } else if(method.IsConstructor) {
                        //create the constructor return type
                        var tempTypeUse = new TypeUse() {
                            Name = method.Name,
                            ParentStatement = method.ParentStatement,
                            Location = method.PrimaryLocation
                        };
                        scopes.AddRange(tempTypeUse.ResolveType());
                    } 
                } else if(match is PropertyDefinition) {
                    scopes.AddRange(((PropertyDefinition)match).ReturnType.ResolveType());
                } else if(match is VariableDeclaration) {
                    scopes.AddRange(((VariableDeclaration)match).VariableType.ResolveType());
                } else {
                    //the only other possibilities are all NamedScopes
                    scopes.Add((NamedScope)match);
                }
            }
            return scopes;
        }


        #region Private Methods
        /// <summary>
        /// Searches for the ImportStatements/AliasStatements that occur prior to this NameUse.
        /// </summary>
        private List<Statement> DetermineAliases() {
            //TODO: do we also need to search base types?
            SourceLocation parentLoc;
            if(ParentStatement.Locations.Count == 1) {
                parentLoc = ParentStatement.PrimaryLocation;
            } else {
                parentLoc = ParentStatement.Locations.First(l => l.Contains(this.Location));
            }
            return ParentStatement.GetAncestors().SelectMany(s => s.GetFileSpecificStatements(parentLoc)).ToList();
        }

        #endregion Private Methods
    }
}
