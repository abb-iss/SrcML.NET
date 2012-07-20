/******************************************************************************
 * Copyright (c) 2011 ABB Group
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
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Globalization;
using System.Diagnostics;

namespace ABB.SrcML.Data
{
	/// <summary>
	/// Call graph that contains pairings between 
	/// </summary>
	public class CallGraph
	{
		private string _xmlFileName;
		private SrcMLFile _document;

		private HashSet<Tuple<int, int>> edgeSet;
        
		private Dictionary<string, int> signatureMap;

        private Dictionary<int, MethodDefinition> methodMap;
        private Dictionary<int, List<MethodCall>> calleeMap;
        private Dictionary<int, List<MethodCall>> callerMap;

		/// <summary>
		/// Gets the document the graph is based on
		/// </summary>
		public SrcMLFile Document
		{
			get
			{
				return this._document;
			}
		}

        /// <summary>
        /// Initializes a new instance of the <see cref="CallGraph"/> class.
        /// </summary>
        /// <param name="xmlFileName">Name of the XML file.</param>
        /// <param name="db">The db.</param>
        public CallGraph(string xmlFileName, SrcMLDataContext db)
        {
            this._xmlFileName = xmlFileName;
            this._document = new SrcMLFile(xmlFileName);

            var methods = FetchMethods(db, xmlFileName);
            this.methodMap = new Dictionary<int, MethodDefinition>();
            this.signatureMap = new Dictionary<string, int>();
            foreach (var method in methods)
            {
                methodMap[method.Id] = method;
                signatureMap[method.MethodSignature] = method.Id;
            }
            this.edgeSet = new HashSet<Tuple<int, int>>(FetchEdgeSet(db, xmlFileName));
            FetchCalleeLists(db);
            FetchCallerLists(db);
        }

        /// <summary>
        /// Determines whether the specified caller contains relationship.
        /// </summary>
        /// <param name="caller">The caller.</param>
        /// <param name="callee">The callee.</param>
        /// <returns>
        ///   <c>true</c> if the specified caller contains relationship; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsRelationship(MethodDefinition caller, MethodDefinition callee)
        {
            if (null == caller)
                throw new ArgumentNullException("caller");
            if (null == callee)
                throw new ArgumentNullException("callee");

            return ContainsRelationship(caller.MethodSignature, callee.MethodSignature);
        }
        
        /// <summary>
        /// Determines whether the specified caller calls the callee.
        /// </summary>
        /// <param name="caller">The caller.</param>
        /// <param name="callee">The callee.</param>
        /// <returns>
        ///   <c>true</c> if the specified caller contains relationship; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsRelationship(string caller, string callee)
        {
            var callerId = signatureMap[caller];
            var calleeId = signatureMap[callee];
            return edgeSet.Contains(new Tuple<int, int>(callerId, calleeId));
        }

        /// <summary>
        /// Gets the callees.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The method definitions that this method calls.</returns>
        public IEnumerable<MethodCall> GetCallees(MethodDefinition method)
        {
            if (null == method)
                throw new ArgumentNullException("method");
            return GetCallees(method.MethodSignature);
        }
        

        /// <summary>
        /// Gets the callees.
        /// </summary>
        /// <param name="methodSignature">The method signature.</param>
        /// <returns>The method definitions that this method calls.</returns>
        public IEnumerable<MethodCall> GetCallees(string methodSignature)
        {
            var methodId = signatureMap[methodSignature];
            return calleeMap[methodId];
        }

        /// <summary>
        /// Gets the callers.
        /// </summary>
        /// <param name="method">The method.</param>
        /// <returns>The method definitions that call this method.</returns>
        public IEnumerable<MethodCall> GetCallers(MethodDefinition method)
        {
            if (null == method)
                throw new ArgumentNullException("method");
            return GetCallers(method.MethodSignature);
        }

        /// <summary>
        /// Gets the callers.
        /// </summary>
        /// <param name="methodSignature">The method signature.</param>
        /// <returns>The method definitions that call this method.</returns>
        public IEnumerable<MethodCall> GetCallers(string methodSignature)
        {
            var methodId = signatureMap[methodSignature];
            return callerMap[methodId];
        }

        # region get the call graph from the database
        private static IEnumerable<Tuple<int, int>> FetchEdgeSet(SrcMLDataContext db, string xmlFileName)
        {
            var pairs = from call in db.MethodCalls
                        where call.CallerDefinition.Archive.Path == xmlFileName
                        select Tuple.Create(call.CallerId, call.CalleeId);
            return pairs;
        }

        private static IEnumerable<MethodDefinition> FetchMethods(SrcMLDataContext db, string xmlFileName)
        {
            var methods = from method in db.Definitions.OfType<MethodDefinition>()
                          where method.Archive.Path == xmlFileName
                          select new
                          {
                              Id = method.Id,
                              FileName = method.FileName,
                              LineNumber = method.LineNumber,
                              ElementXName = method.ElementXName,
                              XPath = method.XPath,
                              MethodClassName = method.MethodClassName,
                              MethodName = method.MethodName,
                              NumberOfMethodParameters = method.NumberOfMethodParameters,
                              NumberOfMethodParametersWithDefaults = method.NumberOfMethodParametersWithDefaults,
                              MethodSignature = method.MethodSignature
                          };
            foreach (var method in methods)
            {
                yield return new MethodDefinition()
                {
                    Id = method.Id,
                    FileName = method.FileName,
                    LineNumber = method.LineNumber,
                    ElementXName = method.ElementXName,
                    XPath = method.XPath,
                    MethodClassName = method.MethodClassName,
                    MethodName = method.MethodName,
                    NumberOfMethodParameters = method.NumberOfMethodParameters,
                    NumberOfMethodParametersWithDefaults = method.NumberOfMethodParametersWithDefaults,
                    MethodSignature = method.MethodSignature
                };
            }
        }
        
        private void FetchCalleeLists(SrcMLDataContext db)
        {
            var results = from method in db.Definitions.OfType<MethodDefinition>()
                          where method.Archive.Path == _xmlFileName
                          let callees = from call in method.CallsFromMethod
                                        orderby call.LineNumber
                                        select Tuple.Create(call.LineNumber, call.CallerId, call.CalleeId)
                          select new KeyValuePair<int, List<Tuple<int,int,int>>>(method.Id, callees.ToList());
            calleeMap = results.ToDictionary(kvp => kvp.Key, kvp => tuplesToMethodCalls(kvp.Value).ToList());
        }

        private void FetchCallerLists(SrcMLDataContext db)
        {
            var results = from method in db.Definitions.OfType<MethodDefinition>()
                          where method.Archive.Path == _xmlFileName
                          let callees = from call in method.CallsToMethod
                                        orderby call.LineNumber
                                        select Tuple.Create(call.LineNumber, call.CallerId, call.CalleeId)
                          select new KeyValuePair<int, List<Tuple<int, int, int>>>(method.Id, callees.ToList());
            callerMap = results.ToDictionary(kvp => kvp.Key, kvp => tuplesToMethodCalls(kvp.Value).ToList());
        }

        private IEnumerable<MethodCall> tuplesToMethodCalls(List<Tuple<int,int,int>> tuples)
        {
            foreach (var tuple in tuples)
                yield return tupleToCall(tuple);
        }

        private MethodCall tupleToCall(Tuple<int,int,int> tuple)
        {
            return new MethodCall()
            {
                LineNumber = tuple.Item1,
                CallerDefinition = methodMap[tuple.Item2],
                CalleeDefinition = methodMap[tuple.Item3]
            };
        }
        #endregion
        
        /// <summary>
        /// Builds the graph.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="xmlFileName">the path to the SrcML document</param>
        internal static IEnumerable<MethodCall> BuildGraph(string connectionString, string xmlFileName)
		{
            Dictionary<Tuple<string, string>, string> scopeMap;
            Dictionary<string, int> idMap;
            Dictionary<Tuple<XName, string, string, int>, List<int>> signatureMap;
            SrcMLFile document;

            using (var db = new SrcMLDataContext(connectionString))
            {
                db.ObjectTrackingEnabled = false;
                db.CommandTimeout = 300;

                var archive = (from a in db.Archives
                               where a.Path == xmlFileName
                               select a).FirstOrDefault();
                if (null == archive)
                    throw new SrcMLDataException("could not find archive " + xmlFileName);

                document = archive.Document;

                idMap = FetchMethodIdMap(db, xmlFileName);
                scopeMap = ValidScope.MakeDictionary(db);
                signatureMap = FetchMethodSignatureMap(db, xmlFileName);
            }

            foreach (var fileUnit in document.FileUnits)
            {
                foreach (var methodCall in BuildGraph(fileUnit, idMap, scopeMap, signatureMap))
                {
                    yield return methodCall;
                }
            }
		}

        /// <summary>
        /// Builds the graph for the specified source file unit.
        /// </summary>
        /// <param name="fileUnit">The file unit.</param>
        /// <param name="methodIdMap">The method id map.</param>
        /// <param name="scopeMap">The scope map.</param>
        /// <param name="methodSignatureMap">The method signature map.</param>
        /// <returns>a collection of method call objects to be inserted into the database</returns>
        internal static IEnumerable<MethodCall> BuildGraph(XElement fileUnit, Dictionary<string, int> methodIdMap,
                                                           Dictionary<Tuple<string, string>, string> scopeMap,
                                                           Dictionary<Tuple<XName, string, string, int>, List<int>> methodSignatureMap)
        {
            var methods = from method in fileUnit.Descendants()
                          where MethodDefinition.ValidNames.Contains(method.Name)
                          select method;
            
            foreach (var method in methods)
            {
                var xpathQuery = method.GetXPath(false);
                var callerId = methodIdMap[xpathQuery];

                var calls = from call in method.Descendants(SRC.Call)
                            select call;
                foreach (var call in calls)
                {
                    var key = MakeKeyForCall(scopeMap, call);

                    List<int> calleeList;
                    if (!methodSignatureMap.TryGetValue(key, out calleeList))
                    {
                        if (null != key.Item2)
                        {
                            key = Tuple.Create(key.Item1, (string) null, key.Item3, key.Item4);
                            if (!methodSignatureMap.TryGetValue(key, out calleeList))
                            {
                                calleeList = null;
                            }
                        }
                    }
                    if (null != calleeList)
                    {
                        foreach (var calleeId in calleeList)
                        {
                            yield return new MethodCall()
                            {
                                CallerId = callerId,
                                CalleeId = calleeId,
                                XPath = call.GetXPath(false),
                                LineNumber = call.GetSrcLineNumber()
                            };
                        }
                    }
                }
            }
        }

        #region Dictionary construction for building the call graph
        private static Dictionary<string, int> FetchMethodIdMap(SrcMLDataContext db, string xmlFileName)
        {
            var methods = from method in db.Definitions.OfType<MethodDefinition>()
                            where method.Archive.Path == xmlFileName
                            select new KeyValuePair<string, int>(method.XPath, method.Id);

            return methods.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private static Dictionary<Tuple<XName, string, string, int>, List<int>> FetchMethodSignatureMap(SrcMLDataContext db, string xmlFileName)
        {
            Dictionary<Tuple<XName, string, string, int>, List<int>> methodMap = new Dictionary<Tuple<XName, string, string, int>, List<int>>();

            var methods = from method in db.Definitions.OfType<MethodDefinition>()
                            where method.Archive.Path == xmlFileName
                            select new
                            {
                                Id = method.Id,
                                ElementType = XName.Get(method.ElementXName),
                                ClassName = method.MethodClassName,
                                Name = method.MethodName,
                                NumberOfParameters = method.NumberOfMethodParameters ?? 0,
                                NumberOfParametersWithDefaults = method.NumberOfMethodParametersWithDefaults ?? 0,
                            };

            foreach (var m in methods)
            {
                for (int i = m.NumberOfParameters - m.NumberOfParametersWithDefaults; i <= m.NumberOfParameters; i++)
                {
                    var key = MakeKey(m.ElementType, m.ClassName, m.Name, i);
                    List<int> values;
                    if (methodMap.TryGetValue(key, out values))
                    {
                        values.Add(m.Id);
                    }
                    else
                    {
                        methodMap.Add(key, new List<int>() { m.Id });
                    }
                }
            }

            return methodMap;
        }
        #endregion

        # region Key Construction for building the graph
        private static Tuple<XName,string, string, int> MakeKey(XName elementType, string className, string methodName, int numberOfParameters)
        {
            return Tuple.Create(elementType, className, methodName, numberOfParameters);
        }

        private static Tuple<XName, string, string, int> MakeKeyForCall(Dictionary<Tuple<string, string>, string> scopeMap, XElement call)
		{
            var methodNameElement = SrcMLHelper.GetNameForMethod(call);
            string methodName = String.Empty;
            if (null != methodNameElement)
            {
                methodName = methodNameElement.Value;
            }

            int numberOfArguments = call.Element(SRC.ArgumentList).Elements(SRC.Argument).Count();

			// if this is a destructor, return a destructor signatureMap
			if (null != call.Element(SRC.Name) && call.Element(SRC.Name).Value.StartsWith("~", StringComparison.OrdinalIgnoreCase))
			{
                return MakeKey(SRC.Destructor, methodNameElement.Value, methodNameElement.Value, numberOfArguments);
			}

            var classNameElement = SrcMLHelper.GetClassNameForMethod(call);
            if (null != classNameElement)
            {
                return MakeKey(SRC.Function, classNameElement.Value, methodName, numberOfArguments);
            }
			// otherwise, find the containing function in case we need it
			var containingFunction = (from parent in call.Ancestors()
									  where MethodDefinition.ValidNames.Contains(parent.Name)
									  select parent).FirstOrDefault();
			
            var className = MethodDefinition.GetParentTypeName(containingFunction);

			// get all of the preceding siblings to this call
			var precedingElements = call.ElementsBeforeSelf();
			if (precedingElements.Any())
			{
				var last = precedingElements.Last();
				var count = precedingElements.Count();

				// if the previous element is a new operator element, just return a new constructor
				if (last.Name == OP.Operator && last.Value == "new")
				{
                    return MakeKey(SRC.Constructor, methodNameElement.Value, methodNameElement.Value, numberOfArguments);
				}

				// else, look for calling object
				if (last.Name == OP.Operator && count > 1 && (last.Value == "." || last.Value == "->"))
				{
					var callingObject = precedingElements.Take(count - 1).Last();
					if ("this" != callingObject.Value)
					{
                        var parentScopePath = (from s in callingObject.Ancestors()
                                               where ScopeDefinition.ValidNames.Contains(s.Name)
                                               select s.GetXPath(false)).FirstOrDefault();
                        string answer;
                        if (scopeMap.TryGetValue(Tuple.Create(parentScopePath, callingObject.Value), out answer))
                        {
                            className = answer;
                        }
					}
				}
			}
            return MakeKey(SRC.Function, className, methodName, numberOfArguments);
        }
        #endregion
    }
}
