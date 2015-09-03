using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace ABB.SrcML {
    public class SrcMLCppAPI {
        [StructLayout(LayoutKind.Sequential, Pack=1)]
        public struct ArchiveAdapter {
            //[MarshalAs(UnmanagedType.U1);
            private IntPtr encoding;
            private IntPtr src_encoding;
            private IntPtr revision;
            private IntPtr language;
            private IntPtr filename;
            private IntPtr url;
            private IntPtr version;
            private IntPtr timestamp;
            private IntPtr hash;
            private int tabstop;
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
            /// Name for the archive being created. This gets set on the <unit>
            /// </summary>
            /// <param name="fname">Chosen name for file</param>
            public void SetArchiveFilename(string fname) {
                filename = Marshal.StringToHGlobalAnsi(fname);
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
            /// TODO
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
            public void srcml_set_hash(string srcHash) {
                hash = Marshal.StringToHGlobalAnsi(srcHash);
            }
            void srcml_set_options(long option) {
                //To be implemented
            }
            //int srcml_disable_option            (long option);
            //int srcml_set_tabstop               (int tabstop);
            //int srcml_register_file_extension   (string  extension, string language);
            //int srcml_register_namespace        (string  prefix, string ns);
            //int srcml_set_processing_instruction(string  target, string data); 
            //int srcml_register_macro            (string  token, string type);
            //int srcml_unparse_set_eol           (int eol);
        }
        public static IntPtr CreatePtrFromStruct(SrcMLCppAPI.ArchiveAdapter ad) {
            int size = Marshal.SizeOf(ad);
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(ad));
            Marshal.StructureToPtr(ad, ptr, false);
            return ptr;
        }
        [DllImport(@"..\..\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SrcmlCreateArchiveFtF(string[] argv, int argc, string outputFile, IntPtr Adapter);
        
        [DllImport(@"..\..\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SrcmlCreateArchiveMtF(string argv, int argc, string outputFile, IntPtr Adapter);
        
        [DllImport(@"..\..\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string SrcmlCreateArchiveFtM(string[] argv, int argc, IntPtr Adapter);
        
        [DllImport(@"..\..\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string SrcmlCreateArchiveMtM(string argv, int argc, IntPtr Adapter);
    }
}