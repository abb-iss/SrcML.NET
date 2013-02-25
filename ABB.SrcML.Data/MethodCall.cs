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
    public class MethodCall : AbstractUse<MethodDefinition> {
        /// <summary>
        /// Creates a new MethodCall object
        /// </summary>
        public MethodCall() {
            Arguments = new Collection<VariableUse>();
            IsConstructor = false;
            IsDestructor = false;
        }

        /// <summary>
        /// The arguments to this call
        /// </summary>
        public Collection<VariableUse> Arguments { get; set; }

        /// <summary>
        /// The calling object for this method
        /// </summary>
        public VariableUse Caller { get; set; }

        /// <summary>
        /// True if this is a call to a constructor
        /// </summary>
        public bool IsConstructor { get; set; }

        /// <summary>
        /// True if this is a call to a destructor
        /// </summary>
        public bool IsDestructor { get; set; }

        /// <summary>
        /// Finds matching <see cref="MethodDefinition">method definitions</see> from the <see cref="ParentScopes"/> of this usage.
        /// Because method calls can also be to constructors and destructors, this will also search for matching types and then constructors
        /// within those types
        /// </summary>
        /// <returns>An enumerable of method definitions that match this method call</returns>
        public override IEnumerable<MethodDefinition> FindMatches() {
            if(IsConstructor || IsDestructor) {
                TypeUse tempTypeUse = new TypeUse() {
                    Name = this.Name,
                    ParentScope = this.ParentScope,
                };

                var matchingMethods = from typeDefinition in tempTypeUse.FindMatches()
                                      from child in typeDefinition.ChildScopes
                                      let method = child as MethodDefinition
                                      where Matches(method)
                                      select method;
                return matchingMethods;
            }
            return base.FindMatches();
        }
        /// <summary>
        /// Tests if the provided method definition matches this method call
        /// </summary>
        /// <param name="definition">The method definition to test</param>
        /// <returns>True if this method call matches the provided method definition</returns>
        public override bool Matches(MethodDefinition definition) {
            if(null == definition) return false;

            var allParametersAreEqual = Enumerable.Zip(this.Arguments, definition.Parameters,
                                                       (a,p) => ArgumentMatchesDefinition(a,p)).All(a => a);

            return this.IsConstructor == definition.IsConstructor &&
                   this.IsDestructor == definition.IsDestructor &&
                   this.Name == definition.Name &&
                   this.Arguments.Count == definition.Parameters.Count &&
                   allParametersAreEqual;
        }

        private bool ArgumentMatchesDefinition(VariableUse argument, ParameterDeclaration parameter) {
            var declarationsForArgument = argument.FindMatches();
            
            return declarationsForArgument.Any() && declarationsForArgument.First().VariableType.Name == parameter.VariableType.Name;
        }
    }
}
