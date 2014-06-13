using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ABB.SrcML.Data {
    public class SwitchStatement : ConditionBlockStatement {
        /// <summary>
        /// The XML name for SwitchStatement
        /// </summary>
        public new const string XmlName = "Switch";

        /// <summary>
        /// Instance method for getting <see cref="SwitchStatement.XmlName"/>
        /// </summary>
        /// <returns>Returns the XML name for SwitchStatement</returns>
        public override string GetXmlName() { return SwitchStatement.XmlName; }

    }
}
