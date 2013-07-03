/******************************************************************************
 * Copyright (c) 2010 ABB Group
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
using NConsoler;
using ABB.SrcML;
using System.IO;
using System.Xml.Linq;
using ABB.SrcML.Utilities;
using System.Collections.ObjectModel;

namespace ABB.SrcML.Tools.Converter
{
    class Program
    {
        static void Main(string[] args)
        {
            Consolery.Run();
        }

        [Action("Converts source code in directory to SrcML")]
        public static void Src2SrcML([Required(Description="Source file or directory of source code to convert")] string source,
                                     [Optional(null, Description=@"The file to write SrcML to. By default, this is <Current Directory>\<Directory>-<Date>.xml
        For instance, if you run .\srcml.exe src2srcml c:\source\python, the resulting output file will be located at .\python-YYYMMDDHHMM.xml")] string outputFileName,
                                     [Optional("Any", Description=@"Language to use. Only files for this language will be included. The options are
        * Any (the default: indicates that all recognized languages should be included)
        * C
        * C++
        * Java
        * AspectJ")] string language,
                                     [Optional("", Description=@"Mapping of file extensions to languages.
        This is formatted like this: ""/languageMapping:ext1=LANG;ext2=LANG;ext3=LANG""
        For example, to map foo.h and foo.cxx to C++, we would use ""/languageMapping:h=C++;cxx=C++""
        All of the languages that are valid for the /language option are valid except for ""Any"".
")] string languageMapping,
                                     [Optional(null, Description=@"Folder with SrcML binaries. If this is not given, the following directories are checked: 
        1. %SRCMLBINDIR%
        2. c:\Program Files (x86)\SrcML\bin
        3. c:\Program Files\SrcML\bin (only checked if c:\Program Files (x86) does not exist)
        4. The current directory")] string binaryFolder)
        {


            if (null == outputFileName)
            {
                var name = source.Split(new char[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).Last();
                outputFileName = String.Format("{0}-{1}.xml", name, DateTime.Now.ToString("yyyyMMddHHmmss"));
            }
            
            SrcMLGenerator generator = null;

            if(null != binaryFolder)
            {
                try
                {
                    generator = GetGeneratorWithDirectory(binaryFolder);
                }
                catch(IOException e)
                {
                    Console.Error.WriteLine("Invalid binary directory: {0}", e.Message);
                    Environment.Exit(-1);
                }
            }
            else
            {
                //try
                //{
                    // check all of the usual suspects
                    generator = new SrcMLGenerator();
                //}
                //catch(IOException)
                //{
                //    generator = null;
                //}
            }

            Language lang = SrcMLElement.GetLanguageFromString(language);
            if (lang > Language.Any)
            {
                Console.WriteLine("Using {0} language", language);
            }

            if (String.Empty != languageMapping)
            {
                foreach (var pair in ParseLanguageMap(languageMapping))
                {
                    Console.WriteLine("Mapping {0} files to {1} language", pair.Extension, KsuAdapter.GetLanguage(pair.Language));
                    string ext = pair.Extension.StartsWith(".") ? pair.Extension : "." + pair.Extension;
                    generator.ExtensionMapping.Add(ext, pair.Language);
                }
            }
            SrcMLFile doc;
            if (Directory.Exists(source)) 
            {
                doc = generator.GenerateSrcMLFileFromDirectory(source, outputFileName, lang);
                Console.WriteLine("Created {0}, a srcML archive, from {1} files located at {2}", doc.FileName, doc.FileUnits.Count(), source);
            }
            else if (File.Exists(source))
            {
                generator.GenerateSrcMLFromFile(source, outputFileName, lang);
                Console.WriteLine("Converted {0} to a srcML document at {1}", source, outputFileName);
            }
            else
            {
                Console.Error.WriteLine("the input folder or directory ({0}) does not exist!", source);
                Environment.Exit(-2);
            }
        }

        [Action("Converts SrcML file back to source code")]
        public static void SrcML2Src([Required(Description="SrcML folder containing the source code")] string fileName,
                                     [Optional(null, Description="Folder to write the source code to. Default: original folder")] string outputFolder)
        {
            string workingFileName = (null == outputFolder ? fileName : Path.GetTempFileName());

            if(null != outputFolder)
                File.Copy(fileName, workingFileName, true);

            try
            {
                SrcMLFile doc = new SrcMLFile(workingFileName);

                if (null != outputFolder)
                    doc.ProjectDirectory = outputFolder;

                try
                {
                    doc.ExportSource();
                }
                catch (IOException e)
                {
                    Console.Error.WriteLine("IO Exception {0}", e.Message);
                }


                //if (1 == errors.Count)
                //    Console.Error.WriteLine("Could not write {0}", errors[0]);
                //else if (1 < errors.Count)
                //{
                //    Console.Error.WriteLine("Could not write the following {0} files:", errors.Count);
                //    foreach (var error in errors)
                //        Console.Error.WriteLine("\t{0}", error);
                //}

                if (null != outputFolder)
                    File.Delete(workingFileName);
            }
            catch (SrcMLException ex)
            {
                Console.Error.WriteLine("Could not read {0}: {1}", fileName, ex.Message);
            }
            catch (IOException ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Action("Lists statistics about the srcML document")]
        public static void Stats([Required(Description="Path to srcML file")] string fileName,
                                 [Optional(null, Description="Path to write output to (standard out by default)")] string outputPath)
        {
            SrcMLFile doc = new SrcMLFile(fileName);
            int numFileUnits = doc.FileUnits.Count();
            var dict = GenerateUnitChildDictionary(doc);
            using (var output = (null == outputPath ? Console.Out : new StreamWriter(outputPath)))
            {
                output.WriteLine("{0} has {1} files containing", Path.GetFileName(doc.FileName), numFileUnits);
                var numChildren = (from list in dict.Values
                                       select list.Count).Sum();
                var sortedKeys = from key in dict.Keys
                                 orderby dict[key].Count descending
                                 select key;

                foreach (var key in sortedKeys)
                {
                    int count = dict[key].Count;
                    var percentage = ((float) count / numChildren) * 100;
                    output.WriteLine("\t{0,10} ({1,5:f2}%) {2} elements", count, percentage, key.LocalName);
                }
            }
        }

        private static IEnumerable<ExtensionLanguagePair> ParseLanguageMap(string languageMapping)
        {
            foreach (var segment in languageMapping.Split(';'))
            {
                var parts = segment.Split('=');
                var extension = parts[0];
                Language language = SrcMLElement.GetLanguageFromString(parts[1]);
                yield return new ExtensionLanguagePair(extension, language);
            }
        }

        private static Dictionary<XName,List<XElement>> GenerateUnitChildDictionary(SrcMLFile document)
        {
            var unitChildDictionary = new Dictionary<XName,List<XElement>>();
            foreach(var unit in document.FileUnits)
            {
                foreach (var child in unit.Elements())
                {
                    List<XElement> nameList = null;
                    if(unitChildDictionary.TryGetValue(child.Name, out nameList))
                    {
                        nameList.Add(child);
                    }
                    else
                    {
                        nameList = new List<XElement>() { child };
                    }
                    unitChildDictionary[child.Name] = nameList;
                }
            }
            return unitChildDictionary;
        }

        private static SrcMLGenerator GetGeneratorWithDirectory(string directory)
        {
            if (!Directory.Exists(directory))
            {
                throw new DirectoryNotFoundException(String.Format("{0} does not exist", directory));
            }
            //if (!File.Exists(runner.ExecutablePath))
            //{
            //    throw new FileNotFoundException(String.Format("{0} does not exist", runner.ExecutablePath));
            //}
            return new SrcMLGenerator(directory);
        }
    }
}
