// Guids.cs
// MUST match guids.h
using System;

namespace ABB.SrcML.VisualStudio.SrcMLService
{
    static class GuidList
    {
        public const string guidSrcMLServicePkgString = "8b448a37-2665-4b23-a2f9-cad4510f1337";
        public const string guidSrcMLServiceCmdSetString = "a92a902c-213b-4b54-9580-afacc7240bec";

        public static readonly Guid guidSrcMLServiceCmdSet = new Guid(guidSrcMLServiceCmdSetString);
    };
}