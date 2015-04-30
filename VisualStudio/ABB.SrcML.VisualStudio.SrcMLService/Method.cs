/******************************************************************************
 * Copyright (c) 2015 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Xiao Qu (ABB Group) - Initial implementation
 *****************************************************************************/

using ABB.SrcML.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ABB.VisualStudio
{

    public class Method
    {
        public string FilePath { get; set; }

        public string NameSpace { get; private set; } //namespace
        
        public string Type { get; private set; } //class         

        public string Name { get; private set; }

        public int StartLineNumber {get; private set;}

        public List<string> ParameterNames { get; private set; }  

        public List<string> ParameterTypes { get; private set; }
                

        public Method()
        {
            FilePath = "";
            NameSpace = "";
            Type = "";
            Name = "";
            StartLineNumber = 0;
            ParameterNames = new List<string>();
            ParameterTypes = new List<string>();
        }
        
        public Method(MethodDefinition methodDef)            
        {
            FilePath = methodDef.PrimaryLocation.SourceFileName;
            NameSpace = methodDef.GetFullName(); //the global level of namespace is the file name
            Type = methodDef.GetAncestors<TypeDefinition>().FirstOrDefault().Name; 
            Name = methodDef.Name;
            StartLineNumber = methodDef.PrimaryLocation.StartingLineNumber;
            SetParameterNames(methodDef);
            SetParameterTypes(methodDef);            
        }
        

        #region Equals

        public static bool operator ==(Method a, Method b) {
            if(System.Object.ReferenceEquals(a, b)) { return true; }
            if(((object) a == null) || ((object) b == null)) { return false; }
            return a.Equals(b);
        }

        public static bool operator !=(Method a, Method b) {
            return !(a == b);
        }

        public override bool Equals(Object obj) {
            if(Object.ReferenceEquals(this, obj)) { return true; }
            return this.Equals(obj as Method);
        }

        public bool Equals(Method b) {
            if(null == b) { return false; }
            return CompareMethodDefinitions(this, b);
        }

        /// <summary>
        /// Determine if two methods have the same signatures (name, paramtertype)
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool SignatureEquals(Method b)
        {
            if (null == b)
            {
                return false;
            }
            return (this.Name == b.Name && ParameterTypesMatch(this.ParameterTypes, b.ParameterTypes));
        }

        private static bool CompareMethodDefinitions(Method a, Method b)
        {
            if (a == null || b == null)
                return false;

            if (a.Name == b.Name
                && ParameterTypesMatch(a.ParameterTypes, b.ParameterTypes)
               )
            {   
                return a.StartLineNumber == b.StartLineNumber;
            }
            else
                return false;
        }

        private static bool ParameterTypesMatch(List<string> paramsA, List<string> paramsB)
        {
            if (paramsA == null || paramsB == null || paramsA.Count != paramsB.Count)
                return false;
            int i = 0;
            foreach (var param in paramsA)
            {
                if (param != paramsB.ElementAt(i))
                    return false;
                i++;
            }
            return true;
        }

        public override int GetHashCode() {
            int result = 17;
            result = result * 31 + Name.GetHashCode();
            result = result * 31 + Type.GetHashCode();
            return result;
        }

        #endregion Equals

        public override string ToString() {
            string parameters = "";
            if(ParameterNames.Count() == 1) {
                parameters = ParameterNames.ToList()[0];
            } else if(ParameterNames.Count() > 1) {
                parameters = ParameterNames.Aggregate((n1, n2) => n1 + ", " + n2);
            }
            return Type + "::" + Name + "(" + parameters + ")";
        }


        private void SetParameterNames(MethodDefinition methodDefinition)
        {
            var parameterNames = from parameter in methodDefinition.Parameters
                                     where parameter.Name != null
                                     select parameter.Name;
            ParameterNames = new List<string>(parameterNames);
        }

        private void SetParameterTypes(MethodDefinition methodDefinition) 
        {            
                var parameterTypeNames = from parameter in methodDefinition.Parameters
                                         where parameter.VariableType != null
                                         select parameter.VariableType.Name;
                ParameterTypes = new List<string>(parameterTypeNames);
        }
        
    }
}