using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using ABB.SrcML;

namespace ABB.SrcML.VisualStudio.TransformTemplate
{
    public class MyTransform : ITransform
    {
        public IEnumerable<XElement> Query(XElement element)
        {
            return Enumerable.Empty<XElement>();
        }

        public XElement Transform(XElement element)
        {
            return element;
        }
    }
}
