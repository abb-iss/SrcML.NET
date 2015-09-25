/* Setup options for srcml unit */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using ABB.SrcML.Utilities;
using System.Diagnostics.Contracts;
using System.IO;
using System.Xml.Linq;
namespace ABB.SrcML {
    /// <summary>
    /// Carries data between C# and C++ for srcML's archives
    /// </summary>

    public class Archive : IDisposable {
        internal SourceData archive;
        internal List<Unit> units;

        /// <summary>
        /// Default ctor allocates a little internal memory
        /// </summary>
        public Archive() {
            archive = new SourceData();
            units = new List<Unit>();
        }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SourceData {
            internal int unitCount;
            internal int tabStop;
            [MarshalAs(UnmanagedType.U4)]
            internal UInt32 optionSet;
            [MarshalAs(UnmanagedType.U4)]
            internal UInt32 optionEnable;
            [MarshalAs(UnmanagedType.U4)]
            internal UInt32 optionDisable;
            internal IntPtr extAndLanguage;
            internal IntPtr prefixAndNamespace;
            internal IntPtr targetAndData;
            internal IntPtr tokenAndType;
            internal IntPtr url;
            internal IntPtr language;
            internal IntPtr version;
            internal IntPtr srcEncoding;
            internal IntPtr xmlEncoding;
            internal IntPtr outputFile;
            internal IntPtr listOfUnits;

        }
        /// <summary>
        /// Set an option to be used by the parser on the archive
        /// </summary>
        /// <param name="srcOption"></param>
        public string OutputFile {
            get { return Marshal.PtrToStringAnsi(archive.outputFile); }
            set { archive.outputFile = Marshal.StringToHGlobalAnsi(value); }
        }

        #region ArchiveMutatorFunctions
        /// <summary>
        /// Set an option to be used by the parser on the archive
        /// </summary>
        /// <param name="srcOption"></param>
        public void SetOptions(UInt32 srcOption) {
            archive.optionSet = srcOption;
        }
        /// <summary>
        /// Set an option to be enabled
        /// </summary>
        /// <param name="srcOption"></param>
        public void EnableOption(UInt32 srcOption) {
            archive.optionEnable |= srcOption;
        }
        /// <summary>
        /// Disable an option
        /// </summary>
        /// <param name="srcOption"></param>
        public void DisableOption(UInt32 srcOption) {
            archive.optionDisable |= srcOption;
        }
        /// <summary>
        /// Set Archive URL
        /// </summary>
        /// <param name="srcurl"></param>
        public void SetArchiveUrl(string srcurl) {
            archive.url = Marshal.StringToHGlobalAnsi(srcurl);
        }
        /// <summary>
        /// Set language for entire archive
        /// </summary>
        /// <param name="lang"></param>
        public void SetArchiveLanguage(string lang) {
            archive.language = Marshal.StringToHGlobalAnsi(lang);
        }
        /// <summary>
        /// Set version of srcML for archive
        /// </summary>
        /// <param name="version"></param>
        public void SetArchiveSrcVersion(string version) {
            archive.version = Marshal.StringToHGlobalAnsi(version);
        }
        /// <summary>
        /// Set encoding for source code
        /// </summary>
        /// <param name="encoding"></param>
        public void SetArchiveSrcEncoding(string encoding) {
            archive.srcEncoding = Marshal.StringToHGlobalAnsi(encoding);
        }
        /// <summary>
        /// Set encoding for srcML's XML output
        /// </summary>
        /// <param name="encoding"></param>
        public void SetArchiveXmlEncoding(string encoding) {
            archive.xmlEncoding = Marshal.StringToHGlobalAnsi(encoding);
        }
        /// <summary>
        /// Set an option to be used by the parser on the archive
        /// </summary>
        /// <param name="srcOption"></param>
        public void SetOutputFile(string outputFile) {
            archive.outputFile = Marshal.StringToHGlobalAnsi(outputFile);
        }
        /// <summary>
        /// Register a file extension to be used with a particular language
        /// </summary>
        /// <param name="extension">The extension string (IE; cpp, cs, java)</param>
        /// <param name="language">Language attributed with extension (IE; C++, C#, Java)</param>
        public void RegisterFileExtension(string extension, string language) {
            archive.extAndLanguage = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

            IntPtr ptr = archive.extAndLanguage;
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(extension));

            ptr += Marshal.SizeOf(typeof(IntPtr));
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(language));
        }
        /// <summary>
        /// Create your own namespace; you may need to do this if you add your own custom tags to srcML archives.
        /// You can also modify known namespaces (like src) to be something else.
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="ns"></param>
        public void RegisterNamespace(string prefix, string ns) {
            archive.prefixAndNamespace = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

            IntPtr ptr = archive.prefixAndNamespace;
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(prefix));

            ptr += Marshal.SizeOf(typeof(IntPtr));
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(ns));
        }
        /// <summary>
        /// Todo (I'm not sure what this function does yet)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="data"></param>
        public void SetProcessingInstruction(string target, string data) {
            archive.targetAndData = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

            IntPtr ptr = archive.targetAndData;
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(target));

            ptr += Marshal.SizeOf(typeof(IntPtr));
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(data));
        }
        /// <summary>
        /// Register a macro so that srcML recognizes it when it finds it in the source code to be parsed to srcML
        /// </summary>
        /// <param name="token"></param>
        /// <param name="type"></param>
        public void RegisterMacro(string token, string type) {
            archive.tokenAndType = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

            IntPtr ptr = archive.tokenAndType;
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(token));

            ptr += Marshal.SizeOf(typeof(IntPtr));
            Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(type));
        }

        /// <summary>
        /// Sets the tabStop for the archive
        /// </summary>
        /// <param name="srcTab"></param>
        public void SetArchiveTabstop(int srcTab) {
            archive.tabStop = srcTab;
        }
        #endregion
        /// <summary>
        /// Marshal's the given object and returns an IntPtr to that object
        /// </summary>
        /// <returns>Pointer to internal data</returns>
        public IntPtr GetPtrToStruct() {
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(archive));
            Marshal.StructureToPtr(archive, ptr, false);
            return ptr;
        }
        /// <summary>
        /// Add a unit to the archive
        /// </summary>
        /// <param name="unit">a srcmlUnit allocated via ABB.SrcML.Unit class</param>
        public void AddUnit(Unit unit) {
            ++unit.referencecount;
            units.Add(unit);
        }
        /// <summary>
        /// Refactor
        /// </summary>
        public void ArchivePack() {
            archive.listOfUnits = Marshal.AllocHGlobal(units.Count * Marshal.SizeOf(typeof(Unit.UnitData)));
            IntPtr unitptr = archive.listOfUnits;
            foreach (Unit str in units) {
                Marshal.StructureToPtr(str.GetInnerStruct(), unitptr, false);
                unitptr += Marshal.SizeOf(typeof(Unit.UnitData));
            }
            archive.unitCount = units.Count;
        }
        /// <summary>
        /// Clean memory allocated to archive
        /// </summary>
        public void Dispose() {
            //Free units
            foreach (Unit uni in units) {
                --uni.referencecount;
                uni.Dispose();
            }
            //Free memory allocated to archive for units
            IntPtr ptr = archive.listOfUnits;
            archive.listOfUnits += Marshal.SizeOf(typeof(Unit.UnitData));
            Marshal.FreeHGlobal(ptr);

            //free archive memory
            Marshal.FreeHGlobal(archive.extAndLanguage);
            Marshal.FreeHGlobal(archive.prefixAndNamespace);
            Marshal.FreeHGlobal(archive.targetAndData);
            Marshal.FreeHGlobal(archive.tokenAndType);
            Marshal.FreeHGlobal(archive.url);
            Marshal.FreeHGlobal(archive.language);
            Marshal.FreeHGlobal(archive.version);
            Marshal.FreeHGlobal(archive.srcEncoding);
            Marshal.FreeHGlobal(archive.xmlEncoding);
        }
    }
    public class Unit : IDisposable {
        internal UnitData unit;
        internal int referencecount;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct UnitData {
            internal IntPtr srcEncoding;
            internal IntPtr revision;
            internal IntPtr language;
            internal IntPtr filenames;
            internal IntPtr url;
            internal IntPtr version;
            internal IntPtr timestamp;
            internal IntPtr hash;
            internal int eol;
            internal IntPtr buffer;
            internal int bufferSize;

        }
        #region UnitMutatorFunctions
        /// <summary>
        /// Sets the encoding for source code
        /// </summary>
        /// <param name="sourceEncoding">The encoding to be set</param>
        public void SetUnitSrcEncoding(string sourceEncoding) {
            unit.srcEncoding = Marshal.StringToHGlobalAnsi(sourceEncoding);
        }
        /// <summary>
        /// Language that srcML should assume for the given document(s)
        /// </summary>
        /// <param name="lang">The chosen language</param>
        public void SetUnitLanguage(string lang) {
            unit.language = Marshal.StringToHGlobalAnsi(lang);
        }

        /// <summary>
        /// Name for the archive being created. This gets set on the <unit>. This version takes a single
        /// file name and points an IntPtr at it.
        /// </summary>
        /// <param name="filename">Chosen name for file</param>
        public void SetUnitFilename(string filename) {
            unit.filenames = Marshal.StringToHGlobalAnsi(filename);
        }
        /// <summary>
        /// URL for namespace in archive
        /// </summary>
        /// <param name="srcUrl">Chosen URL</param>
        public void SetUnitUrl(string srcUrl) {
            unit.url = Marshal.StringToHGlobalAnsi(srcUrl);
        }
        /// <summary>
        /// Version of srcML that generated this archive
        /// </summary>
        /// <param name="srcVersion">Version number</param>
        public void SetUnitSrcVersion(string srcVersion) {
            unit.version = Marshal.StringToHGlobalAnsi(srcVersion);
        }
        /// <summary>
        /// Function takes a string (that represents some source code) and marhals it into a buffer
        /// </summary>
        /// <param name="buf"></param>
        public void SetUnitBuffer(string buf) {
            unit.buffer = Marshal.StringToHGlobalAnsi(buf);
            unit.bufferSize = buf.Length;
        }

        /// <summary>
        /// Timestamp for when archive was generated
        /// </summary>
        /// <param name="srcTimeStamp">The time</param>
        public void SetUnitTimestamp(string srcTimeStamp) {
            unit.timestamp = Marshal.StringToHGlobalAnsi(srcTimeStamp);
        }
        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="srcHash"></param>
        public void SetHash(string srcHash) {
            unit.hash = Marshal.StringToHGlobalAnsi(srcHash);
        }
        /// <summary>
        /// Set end of line marker
        /// </summary>
        /// <param name="srcEol"></param>
        public void UnparseSetEol(int srcEol) {
            unit.eol = srcEol;
        }
        #endregion
        /// <summary>
        /// Clean up manually allocated resources
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(true);
        }
        /// <summary>
        /// Marshal's the given object and returns an IntPtr to that object
        /// </summary>
        /// <returns>Pointer to internal data</returns>
        internal UnitData GetInnerStruct() {
            return unit;
        }
        /// <summary>
        /// Get rid of memory allocated to unit but ONLY if nothing else is referencing it.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing) {
            if (disposing && referencecount == 0) {
                Marshal.FreeHGlobal(unit.srcEncoding);
                Marshal.FreeHGlobal(unit.revision);
                Marshal.FreeHGlobal(unit.language);
                Marshal.FreeHGlobal(unit.filenames);
                Marshal.FreeHGlobal(unit.url);
                Marshal.FreeHGlobal(unit.version);
                Marshal.FreeHGlobal(unit.timestamp);
                Marshal.FreeHGlobal(unit.hash);
                Marshal.FreeHGlobal(unit.buffer);
            }
        }
    }
    /// <summary>
    /// C#-facing API for libsrcml
    /// </summary>
    [CLSCompliant(false)]
    public class LibSrcMLRunner {
        public const string LIBSRCMLPATH = "LibSrcMLWrapper.dll";
        public static readonly Dictionary<Language, string> LanguageEnumDictionary = new Dictionary<Language, string>() {
                {Language.Any, SrcMLLanguages.SRCML_LANGUAGE_NONE },
                {Language.AspectJ, SrcMLLanguages.SRCML_LANGUAGE_JAVA},
                {Language.C, SrcMLLanguages.SRCML_LANGUAGE_C },
                {Language.CPlusPlus, SrcMLLanguages.SRCML_LANGUAGE_CXX },
                {Language.CSharp, SrcMLLanguages.SRCML_LANGUAGE_CSHARP },
                {Language.Java, SrcMLLanguages.SRCML_LANGUAGE_JAVA },
                {Language.ObjectiveC, SrcMLLanguages.SRCML_LANGUAGE_OBJECTIVE_C },
                {Language.XmlLang, SrcMLLanguages.SRCML_LANGUAGE_XML }
        };
        /// <summary>
        /// Options that can be set on a srcML unit or archive
        /// </summary>
        public struct SrcMLOptions {
            /** Create an archive */
            public const UInt32 SRCML_OPTION_ARCHIVE = 1 << 0;
            /** Include line/column position attributes */
            public const UInt32 SRCML_OPTION_POSITION = 1 << 1;
            /** Markup preprocessor elements (default for C, C++, C#) */
            public const UInt32 SRCML_OPTION_CPP_NOMACRO = 1 << 2;
            /** Markup preprocessor elements (default for C, C++) */
            public const UInt32 SRCML_OPTION_CPP = 1 << 2 | 1 << 3;
            /** Issue an XML declaration */
            public const UInt32 SRCML_OPTION_XML_DECL = 1 << 4;
            /** Include any XML namespace declarations */
            public const UInt32 SRCML_OPTION_NAMESPACE_DECL = 1 << 5;
            /** Leave as text preprocessor else parts (default: markup) */
            public const UInt32 SRCML_OPTION_CPP_TEXT_ELSE = 1 << 6;
            /** Markup preprocessor @code #if 0 @endcode sections (default: leave as text) */
            public const UInt32 SRCML_OPTION_CPP_MARKUP_IF0 = 1 << 7;
            /** Apply transformations to the entire srcML file (default: each unit */
            public const UInt32 SRCML_OPTION_APPLY_ROOT = 1 << 8;
            /** Nest if in else if intead of elseif tag */
            public const UInt32 SRCML_OPTION_NESTIF = 1 << 9;
            /** Output hash attribute on each unit (default: on) */
            public const UInt32 SRCML_OPTION_HASH = 1 << 10;
            /** Wrap function/classes/etc with templates (default: on) */
            public const UInt32 SRCML_OPTION_WRAP_TEMPLATE = 1 << 11;
            /** output is interactive (good for editing applications) */
            public const UInt32 SRCML_OPTION_INTERACTIVE = 1 << 12;
            /** Not sure what this used for */
            public const UInt32 SRCML_OPTION_XPATH_TOTAL = 1 << 13;
            /** expression mode */
            public const UInt32 SRCML_OPTION_EXPRESSION = 1 << 14;
            /** Extra processing of @code#line@endcode for position information */
            public const UInt32 SRCML_OPTION_LINE = 1 << 15;
            /** additional cpp:if/cpp:endif checking */
            public const UInt32 SRCML_OPTION_CPPIF_CHECK = 1 << 16;
            /** debug time attribute */
            public const UInt32 SRCML_OPTION_DEBUG_TIMER = 1 << 17;
            /** turn on optional ternary operator markup */
            public const UInt32 SRCML_OPTION_TERNARY = 1 << 18;
            /** turn on optional ternary operator markup */
            public const UInt32 SRCML_OPTION_PSEUDO_BLOCK = 1 << 19;
            /** Turn on old optional markup behaviour */
            public const UInt32 SRCML_OPTION_OPTIONAL_MARKUP = 1 << 20;
            /** Markups literal in special namespace */
            public const UInt32 SRCML_OPTION_LITERAL = 1 << 21;
            /** Markups modifiers in special namespace */
            public const UInt32 SRCML_OPTION_MODIFIER = 1 << 22;
            /** Markups operator in special namespace */
            public const UInt32 SRCML_OPTION_OPERATOR = 1 << 23;
            /** Parser output special tokens for debugging the parser */
            public const UInt32 SRCML_OPTION_DEBUG = 1 << 24;
            /** Markups OpenMP in special namespace */
            public const UInt32 SRCML_OPTION_OPENMP = 1 << 25;
            /** Encode the original source encoding as an attribute */
            public const UInt32 SRCML_OPTION_STORE_ENCODING = 1 << 26;
            public const UInt32 SRCML_OPTION_DEFAULT = (SRCML_OPTION_ARCHIVE | SRCML_OPTION_XML_DECL | SRCML_OPTION_NAMESPACE_DECL | SRCML_OPTION_HASH | SRCML_OPTION_PSEUDO_BLOCK | SRCML_OPTION_TERNARY);
        }
        public struct SrcMLLanguages {
            /* Core language set */
            /** srcML language not set */
            public const string SRCML_LANGUAGE_NONE = "";
            /** string for language C */
            public const string SRCML_LANGUAGE_C = "C";
            /** string for language C++ */
            public const string SRCML_LANGUAGE_CXX = "C++";
            /** string for language Java */
            public const string SRCML_LANGUAGE_JAVA = "Java";
            /** string for language C# */
            public const string SRCML_LANGUAGE_CSHARP = "C#";
            /** string for language C# */
            public const string SRCML_LANGUAGE_OBJECTIVE_C = "Objective-C";
            /** string for language XML */
            public const string SRCML_LANGUAGE_XML = "xml";
        }

        /// <summary>
        /// Generates srcML from a file
        /// </summary>
        /// <param name="fileName">The source file name</param>
        /// <param name="xmlFileName">the output file name</param>
        /// <param name="language">The language to use</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="extensionMapping">an extension mapping</param>
        public void GenerateSrcMLFromFile(string fileName, string xmlFileName, Language language, ICollection<UInt32> namespaceArguments, IDictionary<string, Language> extensionMapping) {
            try {
                GenerateSrcMLFromFiles(new List<string>() { fileName }, xmlFileName, language, namespaceArguments, extensionMapping);
            }
            catch (Exception e) {
                throw new SrcMLException(e.Message, e);
            }
        }
        /// <summary>
        /// Generates srcML from a file
        /// </summary>
        /// <param name="fileNames">An enumerable of filenames</param>
        /// <param name="xmlFileName">the output file name</param>
        /// <param name="language">The language to use</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="extensionMapping">an extension mapping</param>
        public void GenerateSrcMLFromFiles(ICollection<string> fileNames, string xmlFileName, Language language, ICollection<UInt32> namespaceArguments, IDictionary<string, Language> extensionMapping) {
            UInt32 arguments = GenerateArguments(namespaceArguments);
            try {
                using (Archive srcmlArchive = new Archive()) {
                    if (Convert.ToBoolean(extensionMapping.Count())) {
                        srcmlArchive.RegisterFileExtension(extensionMapping.ElementAt(0).Key, extensionMapping.ElementAt(0).Value.ToString());
                    }
                    foreach (string file in fileNames) {
                        using (Unit srcmlUnit = new Unit()) {
                            srcmlUnit.SetUnitFilename(file);
                            srcmlUnit.SetUnitLanguage(LibSrcMLRunner.SrcMLLanguages.SRCML_LANGUAGE_CXX);
                            srcmlArchive.AddUnit(srcmlUnit);
                        }
                    }
                    srcmlArchive.SetOutputFile(xmlFileName);
                    RunSrcML(srcmlArchive, LibSrcMLRunner.SrcmlCreateArchiveFtF);
                }
            }
            catch (Exception e) {
                throw new SrcMLException(e.Message, e);
            }
        }

        /// <summary>
        /// Generates srcML from the given string of source code
        /// </summary>
        /// <param name="source">A single body of source code in a a string</param>
        /// <param name="unitFilename">What name to give the unit</param>
        /// <param name="language">The language</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="omitXmlDeclaration">If true, the XML header is omitted</param>
        /// <returns>The srcML</returns>
        public string GenerateSrcMLFromString(string source, string unitFilename, Language language, ICollection<UInt32> namespaceArguments, bool omitXmlDeclaration) {
            try {
                return GenerateSrcMLFromStrings(new List<string>() { source }, new List<string>() { unitFilename }, language, namespaceArguments, omitXmlDeclaration).ElementAt(0);
            }
            catch (SrcMLException e) {
                throw e;
            }
        }

        /// <summary>
        /// Generates srcML from the given string of source code
        /// </summary>
        /// <param name="sources">list of strings of code (each string is a whole file)</param>
        /// <param name="unitFilename">What name to give the unit</param>
        /// <param name="language">The language</param>
        /// <param name="namespaceArguments">additional arguments</param>
        /// <param name="omitXmlDeclaration">If true, the XML header is omitted</param>
        /// <returns>The srcML</returns>
        public ICollection<string> GenerateSrcMLFromStrings(ICollection<string> sources, ICollection<string> unitFilename, Language language, ICollection<UInt32> namespaceArguments, bool omitXmlDeclaration) {
            Contract.Requires(sources.Count == unitFilename.Count);
            try {
                using (Archive srcmlArchive = new Archive()) {
                    srcmlArchive.SetArchiveLanguage(LanguageEnumDictionary[language]);
                    srcmlArchive.EnableOption(GenerateArguments(namespaceArguments));
                    var sourceandfile = sources.Zip(unitFilename, (src, fle) => new { source = src, file = fle });
                    foreach (var pair in sourceandfile) {
                        using (Unit srcmlUnit = new Unit()) {
                            if (omitXmlDeclaration) {
                                srcmlArchive.DisableOption(LibSrcMLRunner.SrcMLOptions.SRCML_OPTION_XML_DECL);
                            }
                            srcmlUnit.SetUnitBuffer(pair.source);
                            srcmlUnit.SetUnitFilename(pair.file);
                            srcmlUnit.SetUnitLanguage(LanguageEnumDictionary[language]);
                            srcmlArchive.AddUnit(srcmlUnit);
                        }
                    }
                    return RunSrcML(srcmlArchive, LibSrcMLRunner.SrcmlCreateArchiveMtM);
                }
            }
            catch (Exception e) {
                throw new SrcMLException(e.Message, e);
            }
        }

        private List<string> RunSrcML(Archive srcmlArchive, Func<IntPtr[], int, IntPtr> func) {
            srcmlArchive.ArchivePack();

            IntPtr structPtr = srcmlArchive.GetPtrToStruct();

            List<IntPtr> structArrayPtr = new List<IntPtr>();
            structArrayPtr.Add(structPtr);

            IntPtr s = func(structArrayPtr.ToArray(), structArrayPtr.Count());

            if (s == IntPtr.Zero) {
                return new List<string>() { srcmlArchive.OutputFile };
            }

            IntPtr docptr = Marshal.ReadIntPtr(s);
            string docstr = Marshal.PtrToStringAnsi(docptr);

            return new List<string>() { docstr };
        }

        /// <summary>
        /// Take a list of arguments and turn it into a single uint string
        /// </summary>
        /// <param name="namespaceArguments"></param>
        /// <returns>unsigned integer representing srcML argument</returns>
        private static UInt32 GenerateArguments(ICollection<UInt32> namespaceArguments) {
            UInt32 arguments = 0;

            if (namespaceArguments == null) throw new ArgumentNullException("namespaceArguments");

            foreach (var namespaceArgument in namespaceArguments) {
                arguments |= namespaceArgument;
            }

            return arguments;
        }
        #region Low-level API functions
        /// <summary>
        /// Creates archive from a file and reads it out into a file
        /// </summary>
        /// <param name="SourceMetadata">Data about the source; file name, encoding, etc.</param>
        /// <param name="archiveCount">Number of archives to be read</param>
        /// <param name="outputFile">File name for resulting archive</param>
        /// <returns>Error code (see srcML documentation). 0 means nothing went wrong.</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SrcmlCreateArchiveFtF(IntPtr[] SourceMetadata, int archiveCount);
        /// <summary>
        /// Creates archive from memory buffer and reads it out into a file
        /// </summary>
        /// <param name="SourceMetadata">Data about the source; file name, encoding, etc.</param>
        /// <param name="archiveCount">Number of archives to be read</param>
        /// <param name="outputFile">File name for resulting archive</param>
        /// <returns>Error code (see srcML documentation). 0 means nothing went wrong.</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SrcmlCreateArchiveMtF(IntPtr[] SourceMetadata, int archiveCount);
        /// <summary>
        /// Creates archive from File and reads it into a string which gets returned
        /// </summary>
        /// <param name="SourceMetadata"></param>
        /// <param name="archiveCount"></param>
        /// <returns>string representing the archive srcML produced</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr SrcmlCreateArchiveFtM(IntPtr[] SourceMetadata, int archiveCount);
        /// <summary>
        /// Creates archive from memory buffer and returns a separate buffer with resulting srcML
        /// </summary>
        /// <param name="SourceMetadata"></param>
        /// <param name="archiveCount"></param>
        /// <returns>string representing the archive srcML produced</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr SrcmlCreateArchiveMtM(IntPtr[] SourceMetadata, int archiveCount);
        #endregion
        #region Test Functions
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetXmlEncoding(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetSrcEncoding(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetLanguage(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetUrl(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetVersion(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetOptions(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveEnableOption(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveDisableOption(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetTabstop(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveRegisterFileExtension(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveRegisterNamespace(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveSetProcessingInstruction(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestArchiveRegisterMacro(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetFilename(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetLanguage(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetSrcEncoding(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetUrl(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetVersion(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetTimestamp(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetHash(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitUnparseSetEol(IntPtr[] archive);
        /// <summary>
        /// C# to C++ contact point
        /// </summary>
        /// <param name="archive">pointer to srcml archive</param>
        /// <returns></returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool TestUnitSetXmlEncoding(IntPtr[] archive);
        #endregion
    }
}