using ABB.SrcML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Tools.MethodCallSurvey {

    internal interface ILocatable {

        string FullName { get; }

        string Id { get; }

        SrcMLLocation Location { get; }

        string Path { get; }
    }
}