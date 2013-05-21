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
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    /// <summary>
    /// Represents a method call
    /// </summary>
    [Serializable]
    public class MethodCall : AbstractScopeUse<MethodDefinition>, IResolvesToType {
        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            Arguments = new Collection<IResolvesToType>();
            IsConstructor = false;
            IsDestructor = false;
        }

        /// <summary>
        /// The parent scope for this method call. When you update the parent scope, the object also updates the parent scope of <see cref="CallingObject"/> and all of the <see cref="Arguments"/>
        /// </summary>
        public override Scope ParentScope {
            get {
                return base.ParentScope;
            }
            set {
                base.ParentScope = value;
                if(this.CallingObject != null) {
                    this.CallingObject.ParentScope = this.ParentScope;
                }
                foreach(var argument in this.Arguments) {
                    argument.ParentScope = this.ParentScope;
                }
            }
        }

        /// <summary>
        /// The arguments to this call
        /// </summary>
        public Collection<IResolvesToType> Arguments { get; set; }

        /// <summary>
        /// The calling object for a use is used when you have <c>a.Foo()</c> -- this method call would refer to <c>Foo()</c> and the calling object would be <c>a</c>.
        /// </summary>
        public IResolvesToType CallingObject { get; set; }

        /// <summary>
        /// True if this is a call to a constructor
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a call to a destructor
        /// </summary>
        public bool IsDestructor { get; set; }

        /// <summary>
        /// Finds matching <see cref="MethodDefinition">method definitions</see> from the <see cref="Scope.GetParentScopes()"/> of this usage.
        /// Because method calls can also be to constructors and destructors, this will also search for matching types and then constructors
        /// within those types
        /// </summary>
        /// <returns>An enumerable of method definitions that match this method call</returns>
        public override IEnumerable<MethodDefinition> FindMatches() {
            IEnumerable<MethodDefinition> matchingMethods = Enumerable.Empty<MethodDefinition>();

            if(IsConstructor || IsDestructor) {
                TypeUse tempTypeUse = new TypeUse() {
                    Name = this.Name,
                    ParentScope = this.ParentScope,
                };
                tempTypeUse.AddAliases(this.Aliases);

                matchingMethods = from typeDefinition in tempTypeUse.FindMatches()
                                  from method in typeDefinition.GetChildScopesWithId<MethodDefinition>(this.Name)
                                  where Matches(method)
                                  select method;
            } else if(CallingObject != null) {
                matchingMethods = from matchingType in CallingObject.FindMatchingTypes()
                                  from typeDefinition in matchingType.GetParentTypesAndSelf()
                                  from method in typeDefinition.GetChildScopesWithId<MethodDefinition>(this.Name)
                                  where Matches(method)
                                  select method;
            } else {
                var matches = base.FindMatches();
                var matchingTypeMethods = from containingType in ParentScope.GetParentScopesAndSelf<TypeDefinition>()
                                          from typeDefinition in containingType.GetParentTypes()
                                          from method in typeDefinition.GetChildScopesWithId<MethodDefinition>(this.Name)
                                          where Matches(method)
                                          select method;
                matchingMethods = matches.Concat(matchingTypeMethods);
            }
            foreach(var method in matchingMethods) {
                yield return method;
            }
        }

        /// <summary>
        /// Tests if the provided method definition matches this method call
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <returns>True if this method call matches the provided method definition</returns>
        public override bool Matches(MethodDefinition definition) {
            if(null == definition) return false;

            //var argumentsMatchParameters = Enumerable.Zip(this.Arguments, definition.Parameters,
            //                                              (a,p) => ArgumentMatchesDefinition(a,p));

            return this.IsConstructor == definition.IsConstructor &&
                   this.IsDestructor == definition.IsDestructor &&
                   this.Name == definition.Name &&
                   this.Arguments.Count == definition.Parameters.Count;// &&
                   //argumentsMatchParameters.All(a => a);
        }

        /// <summary>
        /// Finds all of the matching type definitions for the return type of this method definition
        /// </summary>
        /// <returns>An enumerable of the matching type definitions for this method</returns>
        public IEnumerable<TypeDefinition> FindMatchingTypes() {
            var possibleReturnTypes = from methodDefinition in FindMatches()
                                      where methodDefinition.ReturnType != null
                                      from typeDefinition in methodDefinition.ReturnType.FindMatches()
                                      select typeDefinition;
            return possibleReturnTypes;
        }

        /// <summary>
        /// Gets the first type definition that matches the return type for this method
        /// </summary>
        /// <returns>The first matching type definition</returns>
        public TypeDefinition FindFirstMatchingType() {
            return FindMatchingTypes().FirstOrDefault();
        }

        /// <summary>
        /// Computes the intersection of the matching types for <paramref name="argument"/> and <paramref name="parameter"/>.
        /// It returns true if the intersection has any elements in it.
        /// </summary>
        /// <param name="argument">an argument from <see cref="Arguments"/></param>
        /// <param name="parameter">a parameter from <see cref="MethodDefinition.Parameters"/></param>
        /// <returns>true if the argument and the parameter have a matching type in common; false otherwise</returns>
        private bool ArgumentMatchesDefinition(IResolvesToType argument, ParameterDeclaration parameter) {
            var possibleArgumentTypes = argument.FindMatchingTypes();
            var possibleParameterTypes = parameter.VariableType.FindMatchingTypes();

            return possibleArgumentTypes.Intersect(possibleParameterTypes).Any();
        }
    }
}
