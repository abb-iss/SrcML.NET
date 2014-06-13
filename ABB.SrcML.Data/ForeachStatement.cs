using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class ForeachStatement : ConditionBlockStatement {
        /// <summary>
        /// The XML name for ForeachStatement
        /// </summary>
        public new const string XmlName = "Foreach";

        public ForeachStatement() : base() {}

        /// <summary>
        /// Instance method for getting <see cref="ForeachStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for ForeachStatement</returns>
        public override string GetXmlName() { return ForeachStatement.XmlName; }
    }
}
