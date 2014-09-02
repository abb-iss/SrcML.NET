/******************************************************************************
 * Copyright (c) 2013 ABB Group
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
using System.Linq;
using System.Xml;

namespace ABB.SrcML.Data {

    /// <summary>
    /// The variable use class represents a use of a variable.
    /// </summary>
    public class VariableUse : NameUse {
        private Expression indexExpression;
        
        /// <summary> The XML name for VariableUse </summary>
        public new const string XmlName = "vu";

        /// <summary> XML Name for <see cref="Index" /> </summary>
        public const string XmlIndexName = "idx";

        /// <summary>
        /// The expression supplied as an index to the variable, if any.
        /// For example, in myVar[17] the index is 17.
        /// </summary>
        public Expression Index {
            get { return indexExpression; }
            set {
                indexExpression = value;
                if(indexExpression != null) {
                    indexExpression.ParentExpression = this;
                    indexExpression.ParentStatement = this.ParentStatement;
                }
            }
        }

        /// <summary> The statement containing this expression. </summary>
        public override Statement ParentStatement {
            get { return base.ParentStatement; }
            set {
                base.ParentStatement = value;
                if(Index != null) { Index.ParentStatement = value; }
            }
        }

        /// <summary>
        /// Returns the child expressions, including the Index.
        /// </summary>
        protected override IEnumerable<AbstractProgramElement> GetChildren() {
            if(Index != null) {
                return base.GetChildren().Concat(Enumerable.Repeat(Index, 1));
            } else {
                return base.GetChildren();
            }
        }

        /// <summary> Returns a string representation of this object. </summary>
        public override string ToString() {
            if(Index != null) {
                return string.Format("{0}[{1}]", base.ToString(), Index);
            } else {
                return base.ToString();
            }
        }
        

        /// <summary>
        /// Gets the first result from <see cref="ResolveType"/>
        /// </summary>
        /// <returns>The first matching variable type definition</returns>
        public TypeDefinition FindFirstMatchingType() {
            return ResolveType().FirstOrDefault();
        }

        /// <summary>
        /// Finds variable declarations that match this name.
        /// </summary>
        /// <returns>An enumerable of possible matches for this variable use.</returns>
        public override IEnumerable<INamedEntity> FindMatches() {
            if(ParentStatement == null) {
                throw new InvalidOperationException("ParentStatement is null");
            }

            if(Name == "this" && ProgrammingLanguage != Language.C) {
                //return nothing, because we don't have a variable declaration to return
                return Enumerable.Empty<INamedEntity>();
            }

            //If there's a prefix, resolve that and search under results
            if(Prefix != null) {
                return Prefix.FindMatches().SelectMany(ns => ns.GetNamedChildren(this.Name)).Where(n => n is VariableDeclaration || n is PropertyDefinition);
            }

            //If there's a calling expression, match and search under results
            var callingScopes = GetCallingScope();
            if(callingScopes != null) {
                IEnumerable<INamedEntity> matches = Enumerable.Empty<INamedEntity>();
                foreach(var scope in callingScopes) {
                    var localMatches = scope.GetNamedChildren(this.Name).Where(n => n is VariableDeclaration || n is PropertyDefinition).ToList();
                    var callingType = scope as TypeDefinition;
                    if(!localMatches.Any() && callingType != null) {
                        //also search under the base types of the calling scope
                        matches = matches.Concat(callingType.SearchParentTypes<INamedEntity>(this.Name, n => n is VariableDeclaration || n is PropertyDefinition));
                    } else {
                        matches = matches.Concat(localMatches);
                    }
                }
                return matches;
            }

            //search enclosing scopes and base types
            foreach(var scope in ParentStatement.GetAncestors()) {
                var matches = scope.GetNamedChildren(this).Where(e => e is VariableDeclaration || e is PropertyDefinition).ToList();
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
                    var baseTypeMatches = typeDef.SearchParentTypes<INamedEntity>(this.Name, e => e is VariableDeclaration || e is PropertyDefinition).ToList();
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
                    where  child is VariableDeclaration || child is PropertyDefinition
                    select child);
        }

        /// <summary>
        /// Finds all of the matching type definitions for all of the variable declarations that
        /// match this variable use
        /// </summary>
        /// <returns>An enumerable of matching type definitions</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {

            IEnumerable<TypeDefinition> typeDefinitions;
            if(this.Name == "this" || 
                (this.Name == "base" && this.ProgrammingLanguage == Language.CSharp) ||
                (this.Name == "super" && this.ProgrammingLanguage == Language.Java)) {
                typeDefinitions = TypeDefinition.GetTypeForKeyword(this);
            } else {
                typeDefinitions = from declaration in FindMatches().OfType<VariableDeclaration>()
                                  where declaration.VariableType != null
                                  from definition in declaration.VariableType.ResolveType()
                                  select definition;
            }

            //TODO: figure out what the type should be when we have an indexer
            //if(Index != null) {
                
            //}

            return typeDefinitions;
        }

        /// <summary>
        /// Instance method for getting <see cref="VariableUse.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for VariableUse</returns>
        public override string GetXmlName() { return VariableUse.XmlName; }

        /// <summary>
        /// Processes the child of the current reader position into a child of this object.
        /// </summary>
        /// <param name="reader">The XML reader</param>
        protected override void ReadXmlChild(XmlReader reader) {
            if(XmlIndexName == reader.Name) {
                Index = XmlSerialization.ReadChildExpression(reader);
            } else {
                base.ReadXmlChild(reader);
            }
        }

        /// <summary>
        /// Writes the contents of this object to <paramref name="writer"/>.
        /// </summary>
        /// <param name="writer">The XML writer to write to</param>
        protected override void WriteXmlContents(XmlWriter writer) {
            if(null != Index) {
                XmlSerialization.WriteElement(writer, Index, XmlIndexName);
            }
            base.WriteXmlContents(writer);
        }
        ///// <summary>
        ///// Tests if this variable usage is a match for
        ///// <paramref name="definition"/></summary>
        ///// <param name="definition">The variable declaration to test</param>
        ///// <returns>true if this matches the variable declaration; false otherwise</returns>
        //public bool Matches(VariableDeclaration definition) {
        //    return definition != null && definition.Name == this.Name;
        //}
    }
}