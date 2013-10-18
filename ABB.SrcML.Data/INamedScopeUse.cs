using System;

namespace ABB.SrcML.Data {

    public interface INamedScopeUse : IUse<INamedScope> {

        INamedScopeUse ChildScopeUse { get; set; }

        INamedScope CreateScope();

        string GetFullName();
    }
}