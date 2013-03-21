// Guids.cs
// MUST match guids.h
using System;

namespace ABB.SrcML.VisualStudio.DataDemo
{
    static class GuidList
    {
        public const string guidDataDemoPkgString = "3b5af89b-e548-461a-a4c9-3b648b2640f7";
        public const string guidDataDemoCmdSetString = "7e4f523c-7466-4c40-b1d9-c7ea50f09c3a";

        public static readonly Guid guidDataDemoCmdSet = new Guid(guidDataDemoCmdSetString);
    };
}