using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
namespace ABB.SrcML {
    public class SrcMLCppAPI {
        [DllImport(@"C:\Users\Christian\Documents\SrcML.NET\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        public static extern int SrcmlCreateArchiveFromListOfFiles(string[] argv, int argc, string outputFile);
        [DllImport(@"C:\Users\Christian\Documents\SrcML.NET\External\srcML1.0\bin\SrcMLCppAPI.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string SrcmlCreateArchiveInMemory(string[] argv, int argc);
    }
}
