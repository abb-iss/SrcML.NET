/* Setup options for srcml unit */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace ABB.SrcML {
    public class LibSrcMLRunner {
        public const string LIBSRCMLPATH = "LibSrcMLWrapper.dll";
        public struct SrcMLOptions {
            /** Create an archive */
            public const ulong SRCML_OPTION_ARCHIVE = 1 << 0;
            /** Include line/column position attributes */
            public const ulong SRCML_OPTION_POSITION = 1 << 1;
            /** Markup preprocessor elements (default for C, C++, C#) */
            public const ulong SRCML_OPTION_CPP_NOMACRO = 1 << 2;
            /** Markup preprocessor elements (default for C, C++) */
            public const ulong SRCML_OPTION_CPP = 1 << 2 | 1 << 3;
            /** Issue an XML declaration */
            public const ulong SRCML_OPTION_XML_DECL = 1 << 4;
            /** Include any XML namespace declarations */
            public const ulong SRCML_OPTION_NAMESPACE_DECL = 1 << 5;
            /** Leave as text preprocessor else parts (default: markup) */
            public const ulong SRCML_OPTION_CPP_TEXT_ELSE = 1 << 6;
            /** Markup preprocessor @code #if 0 @endcode sections (default: leave as text) */
            public const ulong SRCML_OPTION_CPP_MARKUP_IF0 = 1 << 7;
            /** Apply transformations to the entire srcML file (default: each unit */
            public const ulong SRCML_OPTION_APPLY_ROOT = 1 << 8;
            /** Nest if in else if intead of elseif tag */
            public const ulong SRCML_OPTION_NESTIF = 1 << 9;
            /** Output hash attribute on each unit (default: on) */
            public const ulong SRCML_OPTION_HASH = 1 << 10;
            /** Wrap function/classes/etc with templates (default: on) */
            public const ulong SRCML_OPTION_WRAP_TEMPLATE = 1 << 11;
            /** output is interactive (good for editing applications) */
            public const ulong SRCML_OPTION_INTERACTIVE = 1 << 12;
            /** Not sure what this used for */
            public const ulong SRCML_OPTION_XPATH_TOTAL = 1 << 13;
            /** expression mode */
            public const ulong SRCML_OPTION_EXPRESSION = 1 << 14;
            /** Extra processing of @code#line@endcode for position information */
            public const ulong SRCML_OPTION_LINE = 1 << 15;
            /** additional cpp:if/cpp:endif checking */
            public const ulong SRCML_OPTION_CPPIF_CHECK = 1 << 16;
            /** debug time attribute */
            public const ulong SRCML_OPTION_DEBUG_TIMER = 1 << 17;
            /** turn on optional ternary operator markup */
            public const ulong SRCML_OPTION_TERNARY = 1 << 18;
            /** turn on optional ternary operator markup */
            public const ulong SRCML_OPTION_PSEUDO_BLOCK = 1 << 19;
            /** Turn on old optional markup behaviour */
            public const ulong SRCML_OPTION_OPTIONAL_MARKUP = 1 << 20;
            /** Markups literal in special namespace */
            public const ulong SRCML_OPTION_LITERAL = 1 << 21;
            /** Markups modifiers in special namespace */
            public const ulong SRCML_OPTION_MODIFIER = 1 << 22;
            /** Markups operator in special namespace */
            public const ulong SRCML_OPTION_OPERATOR = 1 << 23;
            /** Parser output special tokens for debugging the parser */
            public const ulong SRCML_OPTION_DEBUG = 1 << 24;
            /** Markups OpenMP in special namespace */
            public const ulong SRCML_OPTION_OPENMP = 1 << 25;
            /** Encode the original source encoding as an attribute */
            public const ulong SRCML_OPTION_STORE_ENCODING = 1 << 26;
            public const ulong SRCML_OPTION_DEFAULT = (SRCML_OPTION_ARCHIVE | SRCML_OPTION_XML_DECL | SRCML_OPTION_NAMESPACE_DECL | SRCML_OPTION_HASH | SRCML_OPTION_PSEUDO_BLOCK | SRCML_OPTION_TERNARY);

            /* Core language set */
            /** srcML language not set */
            public const int SRCML_LANGUAGE_NONE = 0;
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
        /// Carries data between C# and C++ for srcML's archives
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SourceData : IDisposable {
            private IntPtr encoding;
            private IntPtr src_encoding;
            private IntPtr revision;
            private IntPtr language;
            private IntPtr filenames;
            private IntPtr url;
            private IntPtr version;
            private IntPtr timestamp;
            private IntPtr hash;
            private IntPtr buffer;
            private int bufferCount;
            private IntPtr bufferSize;
            private int tabstop;
            private ulong optionset;
            private ulong optionenable;
            private ulong optiondisable;
            private IntPtr ExtAndLanguage;
            private IntPtr PrefixAndNamespace;
            private IntPtr TargetAndData;
            private IntPtr TokenAndType;
            private int eol;

            /// <summary>
            /// Sets the encoding for source code
            /// </summary>
            /// <param name="sourceEncoding">The encoding to be set</param>
            public void SetArchiveSrcEncoding(string sourceEncoding) {
                src_encoding = Marshal.StringToHGlobalAnsi(sourceEncoding);
            }
            /// <summary>
            /// Sets the xml encoding for the archive
            /// </summary>
            /// <param name="xmlEncoding">The chosen encoding</param>
            public void SetArchiveXmlEncoding(string xmlEncoding) {
                encoding = Marshal.StringToHGlobalAnsi(xmlEncoding);
            }
            /// <summary>
            /// Language that srcML should assume for the given document(s)
            /// </summary>
            /// <param name="lang">The chosen language</param>
            public void SetArchiveLanguage(string lang) {
                language = Marshal.StringToHGlobalAnsi(lang);
            }
            /// <summary>
            /// Name for the archive being created. This gets set on the <unit>. This version takes a list
            /// of file names. These must be in order (in synch with the buffer's list). Otherwise, the
            /// wrong file name will be assigned to units.
            /// </summary>
            /// <param name="fname">Chosen name for file</param>
            public void SetArchiveFilename(List<String> fileList) {
                filenames = Marshal.AllocHGlobal(fileList.Count * Marshal.SizeOf(typeof(IntPtr)));
                IntPtr ptr = filenames;
                foreach (string str in fileList) {
                    Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(str));
                    ptr += Marshal.SizeOf(typeof(IntPtr));
                }
            }
            /// <summary>
            /// Name for the archive being created. This gets set on the <unit>. This version takes a single
            /// file name and points an IntPtr at it.
            /// </summary>
            /// <param name="fname">Chosen name for file</param>
            public void SetArchiveFilename(String filename) {
                filenames = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(IntPtr)));
                IntPtr ptr = filenames;
                Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(filename));
                bufferCount = 1;
            }
            /// <summary>
            /// URL for namespace in archive
            /// </summary>
            /// <param name="srcurl">Chosen URL</param>
            public void SetArchiveUrl(string srcurl) {
                url = Marshal.StringToHGlobalAnsi(srcurl);
            }
            /// <summary>
            /// Version of srcML that generated this archive
            /// </summary>
            /// <param name="srcVersion">Version number</param>
            public void SetArchiveSrcVersion(string srcVersion) {
                version = Marshal.StringToHGlobalAnsi(srcVersion);
            }
            /// <summary>
            /// Function takes a list of strings (that represent some source code) and manually lays it out into a 
            /// two dimensional array that can be passed into the cpp .dll
            /// </summary>
            /// <param name="bufferList"></param>
            public void SetArchiveBuffer(List<String> bufferList) {
                buffer = Marshal.AllocHGlobal(bufferList.Count * Marshal.SizeOf(typeof(IntPtr)));
                bufferSize = Marshal.AllocHGlobal(bufferList.Count * Marshal.SizeOf(typeof(IntPtr)));
                IntPtr buffptr = buffer;
                IntPtr numptr = bufferSize;
                int i = 0;
                foreach (string str in bufferList) {
                    Marshal.WriteIntPtr(buffptr, Marshal.StringToHGlobalAnsi(str));
                    buffptr += Marshal.SizeOf(typeof(IntPtr));
                    Marshal.WriteIntPtr(numptr, new IntPtr(str.Length));
                    numptr += Marshal.SizeOf(typeof(IntPtr));
                    ++i;
                }
                bufferCount = bufferList.Count();
            }
            /// <summary>
            /// Sets the tabstop for the archive
            /// </summary>
            /// <param name="srctab"></param>
            public void SetArchiveTabstop(int srctab) {
                tabstop = srctab;
            }
            /// <summary>
            /// Timestamp for when archive was generated
            /// </summary>
            /// <param name="srcTimestamp">The time</param>
            public void SetArchiveTimestamp(string srcTimestamp) {
                timestamp = Marshal.StringToHGlobalAnsi(srcTimestamp);
            }
            /// <summary>
            /// TODO
            /// </summary>
            /// <param name="srcHash"></param>
            public void SetHash(string srcHash) {
                hash = Marshal.StringToHGlobalAnsi(srcHash);
            }
            /// <summary>
            /// Set an option to be used by the parser on the archive
            /// </summary>
            /// <param name="option"></param>
            public void SetOptions(ulong srcoption) {
                optionset = srcoption;
            }
            /// <summary>
            /// Set an option to be enabled
            /// </summary>
            /// <param name="option"></param>
            public void EnableOption(ulong srcoption) {
                optionenable = srcoption;
            }
            /// <summary>
            /// Disable an option
            /// </summary>
            /// <param name="option"></param>
            public void DisableOption(ulong srcoption) {
                optiondisable = srcoption;
            }
            /// <summary>
            /// Register a file extension to be used with a particular language
            /// </summary>
            /// <param name="extension">The extension string (IE; cpp, cs, java)</param>
            /// <param name="language">Language attributed with extension (IE; C++, C#, Java)</param>
            public void RegisterFileExtension(string extension, string language) {
                ExtAndLanguage = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));
               
                IntPtr ptr = ExtAndLanguage;
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
                PrefixAndNamespace = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

                IntPtr ptr = PrefixAndNamespace;
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
                TargetAndData = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

                IntPtr ptr = TargetAndData;
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
                TokenAndType = Marshal.AllocHGlobal(2 * Marshal.SizeOf(typeof(IntPtr)));

                IntPtr ptr = ExtAndLanguage;
                Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(token));

                ptr += Marshal.SizeOf(typeof(IntPtr));
                Marshal.WriteIntPtr(ptr, Marshal.StringToHGlobalAnsi(type));
            }
            /// <summary>
            /// Set end of line marker
            /// </summary>
            /// <param name="eol"></param>
            public void UnparseSetEol(int srceol) {
                eol = srceol;
            }
            /// <summary>
            /// Clean up manually allocated resources
            /// </summary>
            public void Dispose() {
                Marshal.FreeHGlobal(buffer);
                Marshal.FreeHGlobal(bufferSize);
                Marshal.FreeHGlobal(filenames);
                GC.SuppressFinalize(this);
            }
        }
        public static IntPtr CreatePtrFromStruct(LibSrcMLRunner.SourceData ad) {
            int size = Marshal.SizeOf(ad);
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(ad));
            Marshal.StructureToPtr(ad, ptr, false);
            return ptr;
        }
        /// <summary>
        /// Creates archive from a file and reads it out into a file
        /// </summary>
        /// <param name="SourceMetadata">Data about the source; file name, encoding, etc.</param>
        /// <param name="archiveCount">Number of archives to be read</param>
        /// <param name="outputFile">File name for resulting archive</param>
        /// <returns>Error code (see srcML documentation). 0 means nothing went wrong.</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SrcmlCreateArchiveFtF(IntPtr[] SourceMetadata, int archiveCount, string outputFile);
        /// <summary>
        /// Creates archive from memory buffer and reads it out into a file
        /// </summary>
        /// <param name="SourceMetadata">Data about the source; file name, encoding, etc.</param>
        /// <param name="archiveCount">Number of archives to be read</param>
        /// <param name="outputFile">File name for resulting archive</param>
        /// <returns>Error code (see srcML documentation). 0 means nothing went wrong.</returns>
        [DllImport(LIBSRCMLPATH, CallingConvention = CallingConvention.Cdecl)]
        public static extern int SrcmlCreateArchiveMtF(IntPtr[] SourceMetadata, int archiveCount, string outputFile);
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
    }
}
