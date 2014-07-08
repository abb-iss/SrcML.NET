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
        /// Searches through the <see cref="IScope.DeclaredVariables"/> to see if any of them
        /// <see cref="Matches(IVariableDeclaration)">matches</see>
        /// </summary>
        /// <returns>An enumerable of matching variable declarations.</returns>
        public IEnumerable<VariableDeclaration> FindMatches() {
            //TODO: review this method and update it for changes in TypeUse structure
            throw new NotImplementedException();
            //IEnumerable<VariableDeclaration> matchingVariables = Enumerable.Empty<VariableDeclaration>();

            //if(CallingObject != null) {
            //    matchingVariables = from matchingType in CallingObject.FindMatchingTypes()
            //                        from typeDefinition in matchingType.GetParentTypesAndSelf()
            //                        from variable in typeDefinition.DeclaredVariables
            //                        where Matches(variable)
            //                        select variable;
            //} else {
            //    var matches = from scope in ParentScopes
            //                  from variable in scope.DeclaredVariables
            //                  where Matches(variable)
            //                  select variable;

            //    var parameterMatches = from method in ParentScope.GetParentScopesAndSelf<MethodDefinition>()
            //                           from parameter in method.Parameters
            //                           where Matches(parameter)
            //                           select parameter;

            //    var matchingParentVariables = from containingType in ParentScope.GetParentScopesAndSelf<TypeDefinition>()
            //                                  from typeDefinition in containingType.GetParentTypes()
            //                                  from variable in typeDefinition.DeclaredVariables
            //                                  where Matches(variable)
            //                                  select variable;
            //    matchingVariables = matches.Concat(matchingParentVariables).Concat(parameterMatches);
            //}
            //return matchingVariables;
        }

        /// <summary>
        /// Finds all of the matching type definitions for all of the variable declarations that
        /// match this variable use
        /// </summary>
        /// <returns>An enumerable of matching type definitions</returns>
        public override IEnumerable<TypeDefinition> ResolveType() {
            //TODO: implement ResolveType
            return Enumerable.Empty<TypeDefinition>();

            //IEnumerable<TypeDefinition> typeDefinitions;
            //if(this.Name == "this" || (this.Name == "base" && this.ProgrammingLanguage == Language.CSharp)) {
            //    typeDefinitions = TypeDefinition.GetTypeForKeyword(this);
            //} else {
            //    var matchingVariables = FindMatches();
            //    if(matchingVariables.Any()) {
            //        typeDefinitions = from declaration in matchingVariables
            //                          where declaration.VariableType != null
            //                          from definition in declaration.VariableType.FindMatches()
            //                          select definition;
            //    } else {
            //        var tempTypeUse = new TypeUse() {
            //            Name = this.Name,
            //            ParentScope = this.ParentScope,
            //            ProgrammingLanguage = this.ProgrammingLanguage,
            //        };
            //        if(CallingObject != null && CallingObject is VariableUse) {
            //            var caller = CallingObject as VariableUse;
            //            Stack<NamedScopeUse> callerStack = new Stack<NamedScopeUse>();
            //            while(caller != null) {
            //                var scopeUse = new NamedScopeUse() {
            //                    Name = caller.Name,
            //                    ProgrammingLanguage = this.ProgrammingLanguage,
            //                };
            //                callerStack.Push(scopeUse);
            //                caller = caller.CallingObject as VariableUse;
            //            }

            //            NamedScopeUse prefix = null, last = null;

            //            foreach(var current in callerStack) {
            //                if(null == prefix) {
            //                    prefix = current;
            //                    last = prefix;
            //                } else {
            //                    last.ChildScopeUse = current;
            //                    last = current;
            //                }
            //            }
            //            prefix.ParentScope = this.ParentScope;
            //            tempTypeUse.Prefix = prefix;
            //        }
            //        tempTypeUse.AddAliases(this.Aliases);
            //        typeDefinitions = tempTypeUse.FindMatchingTypes();
            //    }
            //}
            //return typeDefinitions;
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