using ABB.SrcML.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ABB.SrcML.Tools.DataTester {

    internal class Program {

        private static void GenerateData(string sourcePath, string dataPath, string csvDirectory) {
            //Dictionary<Language, AbstractCodeParser> CodeParser = new Dictionary<Language, AbstractCodeParser>() {
            //    { Language.CPlusPlus, new CPlusPlusCodeParser() },
            //    { Language.Java, new JavaCodeParser() },
            //    { Language.CSharp, new CSharpCodeParser() }
            //};

            //string fileLogPath = Path.Combine(dataPath, "parse.log");
            //string callLogPath = Path.Combine(dataPath, "methodcalls.log");
            //string csvPath = Path.Combine(csvDirectory, "timing.csv");
            //string jsonPath = String.Format("{0}.json", Path.Combine(@"c:\Workspace\DataVisualization", dataPath.Substring(23)));

            //if(!Directory.Exists(sourcePath)) {
            //    Console.Error.WriteLine("{0} does not exist", sourcePath);
            //    return;
            //}

            //if(File.Exists(callLogPath)) {
            //    File.Delete(callLogPath);
            //}
            //if(File.Exists(fileLogPath)) {
            //    File.Delete(fileLogPath);
            //}

            //var archive = new SrcMLArchive(dataPath);
            //archive.XmlGenerator.ExtensionMapping[".cxx"] = Language.CPlusPlus;
            //archive.XmlGenerator.ExtensionMapping[".c"] = Language.CPlusPlus;
            //archive.XmlGenerator.ExtensionMapping[".cc"] = Language.CPlusPlus;
            //archive.XmlGenerator.ExtensionMapping[".hpp"] = Language.CPlusPlus;

            //AbstractFileMonitor monitor = new FileSystemFolderMonitor(sourcePath, dataPath, new LastModifiedArchive(dataPath), archive);

            //ManualResetEvent mre = new ManualResetEvent(false);
            //Stopwatch timer = new Stopwatch();
            //bool startupCompleted = false;

            //monitor.UpdateArchivesCompleted  += (o, e) => {
            //    timer.Stop();
            //    startupCompleted = true;
            //    mre.Set();
            //};

            //timer.Start();
            //var task = monitor.UpdateArchivesAsync();
            
            //string[] spinner = new string[] { "\\\r", "|\r", "/\r" };
            //int spinner_index = -1;
            //while(!startupCompleted) {
            //    spinner_index = (++spinner_index) % 3;
            //    Console.Write("Updating archive for {0}... {1}", sourcePath, spinner[spinner_index]);
            //    startupCompleted = mre.WaitOne(5000);
            //}
            //timer.Stop();
            //Console.WriteLine("Updating archive for {0}... {1}", sourcePath, timer.Elapsed);

            //NamespaceDefinition globalScope = null;
            //timer.Reset();

            //int numberOfFailures = 0;
            //int numberOfSuccesses = 0;
            //int numberOfFiles = 0;
            //Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

            //if(!File.Exists(csvPath)) {
            //    File.WriteAllLines(csvPath, new string[] { String.Join(",", "Project", "Files", "Failures", "Time (s)") });
            //}
            //using(StreamWriter fileLog = new StreamWriter(fileLogPath), csvFile = new StreamWriter(csvPath, true)) {
            //    timer.Start();
            //    foreach(var unit in archive.FileUnits) {
            //        var fileName = SrcMLElement.GetFileNameForUnit(unit);
            //        var language = SrcMLElement.GetLanguageForUnit(unit);

            //        try {
            //            var scopeForUnit = CodeParser[language].ParseFileUnit(unit);

            //            if(null == globalScope) {
            //                globalScope = scopeForUnit;
            //            } else {
            //                globalScope = globalScope.Merge(scopeForUnit);
            //            }
            //            timer.Stop();
            //            fileLog.WriteLine("Parsing {0} PASSED", fileName);
            //            numberOfSuccesses++;
            //        } catch(Exception e) {
            //            timer.Stop();
            //            fileLog.WriteLine("Parsing {0} FAILED", fileName);
            //            fileLog.WriteLine(e.StackTrace);
            //            var key = e.StackTrace.Split('\n')[0].Trim();
            //            if(!errors.ContainsKey(key)) {
            //                errors[key] = new List<string>();
            //            }
            //            errors[key].Add(fileName);

            //            numberOfFailures++;
            //        } finally {
            //            if(++numberOfFiles % 50 == 0) {
            //                Console.Write("{0,5:N0} files completed in {1} with {2,5:N0} failures\r", numberOfFiles, timer.Elapsed, numberOfFailures);
            //                csvFile.WriteLine(string.Join(",", sourcePath, numberOfFiles, numberOfFailures, timer.Elapsed.TotalSeconds));
            //            }
            //            timer.Start();
            //        }
            //    }
            //}
            //timer.Stop();
            //Console.WriteLine("{0,5:N0} files completed in {1} with {2,5:N0} failures", numberOfFiles, timer.Elapsed, numberOfFailures);

            //Console.WriteLine("\nSummary");
            //Console.WriteLine("===================");

            //Console.WriteLine("{0,10:N0} failures  ({1,8:P2})", numberOfFailures, ((float) numberOfFailures) / numberOfFiles);
            //Console.WriteLine("{0,10:N0} successes ({1,8:P2})", numberOfSuccesses, ((float) numberOfSuccesses) / numberOfFiles);
            //Console.WriteLine("{0} to generate data", timer.Elapsed);
            //Console.WriteLine("See parse log at {0}", fileLogPath);

            //OutputCallGraphByType(globalScope, jsonPath);

            //PrintScopeReport(globalScope, sourcePath, csvDirectory);
            //PrintMethodCallReport(globalScope, sourcePath, csvDirectory, callLogPath);
            //TODO reimplement once merge has been implemented
            throw new NotImplementedException();
        }

        private static void Main(string[] args) {
            var projects = ReadMapping(@"C:\Workspace\source-srcmldata-mapping.txt");
            foreach(var project in projects) {
                GenerateData(project.Key, project.Value, @"c:\Workspace\SrcMLData");
            }
        }

        private static void OutputCallGraphByType(NamespaceDefinition globalScope, string jsonPath) {
            //TODO reimplement once MethodCalls has been reimplemented
            //using(var writer = new JsonTextWriter(new StreamWriter(jsonPath))) {
            //    writer.WriteStartArray();
            //    foreach(var typeDefinition in globalScope.GetDescendantsAndSelf<TypeDefinition>()) {
            //        writer.WriteStartObject();
            //        writer.WritePropertyName("name");
            //        writer.WriteValue(typeDefinition.GetFullName());

            //        var calls = from scope in typeDefinition.GetDescendantsAndSelf()
            //                    from call in scope.MethodCalls
            //                    select call;

            //        writer.WritePropertyName("size");
            //        writer.WriteValue(calls.Count());

            //        // find the parent type of all the calls
            //        var callMatches = from call in calls
            //                          let match = call.FindMatches().FirstOrDefault()
            //                          where match != null
            //                          let parentOfMatch = match.GetFirstParent<ITypeDefinition>()
            //                          where parentOfMatch != null
            //                          select parentOfMatch.GetFullName();
            //        // output the calls property and array
            //        writer.WritePropertyName("calls");
            //        writer.WriteStartArray();
            //        foreach(var call in callMatches) {
            //            writer.WriteValue(call);
            //        }
            //        writer.WriteEndArray();
            //        writer.WriteEndObject();
            //    }
            //    writer.WriteEndArray();
            //}
            throw new NotImplementedException();
        }

        private static void PrintMethodCallReport(NamespaceDefinition globalScope, string sourcePath, string csvDirectory, string callLogPath) {
            //TODO reimpleement once visitors have been modified
            //var csvPath = Path.Combine(csvDirectory, "methodcalls.csv");
            //Console.WriteLine("\nMethod Call Report");
            //Console.WriteLine("===============");
            //var methodCalls = from scope in VariableScopeIterator.Visit(globalScope)
            //                  from call in scope.MethodCalls
            //                  select call;

            //int numMethodCalls = 0;
            //int numMatchedMethodCalls = 0;
            //Stopwatch sw = new Stopwatch();

            //using(var callLog = new StreamWriter(callLogPath)) {
            //    foreach(var call in methodCalls) {
            //        sw.Start();
            //        var match = call.FindMatches().FirstOrDefault();
            //        sw.Stop();
            //        numMethodCalls++;
            //        if(null != match) {
            //            numMatchedMethodCalls++;
            //            callLog.WriteLine("{0} ({1}:{2}) -> {3} ({4}:{5})", call.Name, call.Location.SourceFileName, call.Location.StartingLineNumber, match.Name, match.PrimaryLocation.SourceFileName, match.PrimaryLocation.StartingLineNumber);
            //        }
            //    }
            //}

            //Console.WriteLine("{0,10:N0} method calls", numMethodCalls);
            //Console.WriteLine("{0,10:N0} matched method calls ({1,8:P2})", numMatchedMethodCalls, ((float) numMatchedMethodCalls) / numMethodCalls);
            //Console.WriteLine("{0,10:N0} matches / millisecond ({1,7:N0} ms elapsed)", ((float) numMethodCalls) / sw.ElapsedMilliseconds, sw.ElapsedMilliseconds);
            //Console.WriteLine("See matched method calls in {0}", callLogPath);

            //if(!File.Exists(csvPath)) {
            //    File.WriteAllText(csvPath, String.Format("{0}{1}", String.Join(",", "Project", "Method Calls", "Matched Method Calls", "Time (ms)"), Environment.NewLine));
            //}
            //File.AppendAllText(csvPath, String.Format("{0}{1}", String.Join(",", sourcePath, numMethodCalls, numMatchedMethodCalls, sw.ElapsedMilliseconds), Environment.NewLine));
            throw new NotImplementedException();
        }

        private static void PrintScopeReport(NamespaceDefinition globalScope, string sourcePath, string csvDirectory) {
            //var csvPath = Path.Combine(csvDirectory, "scopes.csv");
            //Console.WriteLine("\nScope Report");
            //Console.WriteLine("===============");

            //var allScopes = VariableScopeIterator.Visit(globalScope);
            //int numScopes = allScopes.Count();
            //int numNamedScopes = allScopes.OfType<INamedScope>().Count();
            //int numNamespaces = allScopes.OfType<NamespaceDefinition>().Count();
            //int numTypes = allScopes.OfType<ITypeDefinition>().Count();
            //int numMethods = allScopes.OfType<IMethodDefinition>().Count();

            //Console.WriteLine("{0,10:N0} scopes", numScopes);

            //Console.WriteLine("{0,10:N0} named scopes", numNamedScopes);

            //Console.WriteLine("{0,10:N0} namespaces", numNamespaces);
            //Console.WriteLine("{0,10:N0} types", numTypes);
            //Console.WriteLine("{0,10:N0} methods", numMethods);
            //if(!File.Exists(csvPath)) {
            //    File.WriteAllText(csvPath, String.Format("{0}{1}", String.Join(",", "Project", "Scopes", "Named Scopes", "Namespaces", "Types", "Methods"), Environment.NewLine));
            //}
            //File.AppendAllText(csvPath, String.Format("{0}{1}", String.Join(",", sourcePath, numScopes, numNamedScopes, numNamespaces, numTypes, numMethods), Environment.NewLine));
            throw new NotImplementedException();
        }

        private static Dictionary<string, string> ReadMapping(string mappingFilePath) {
            var pairs = from line in File.ReadAllLines(mappingFilePath)
                        let parts = line.Split('|')
                        where parts.Length == 2
                        select new { Key = parts[0], Value = parts[1] };
            var mapping = new Dictionary<string, string>(pairs.Count());
            foreach(var pair in pairs) {
                mapping.Add(pair.Key, pair.Value);
            }
            return mapping;
        }
    }
}