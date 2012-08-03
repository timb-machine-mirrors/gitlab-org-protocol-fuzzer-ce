using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;


namespace WSDLToPeach
{
    class XMLEmitter
    {
        protected XmlDocument xdoc;

        public XMLEmitter()
        {
            xdoc = new XmlDocument();
        }

        protected XmlAttribute CreateAttribute(string name, string local, string ns, string value)
        {
            XmlAttribute xmlAttribute = xdoc.CreateAttribute(name, local, ns);
            xmlAttribute.Value = value;
            return xmlAttribute;
        }

        protected XmlAttribute CreateAttribute(string name, string local, string value)
        {
            XmlAttribute xmlAttribute = xdoc.CreateAttribute(name, local, value);
            return xmlAttribute;
        }

        protected XmlAttribute CreateAttribute(string name, string value)
        {
            XmlAttribute xmlAttribute = xdoc.CreateAttribute(name);
            xmlAttribute.Value = value;
            return xmlAttribute;
        }

        

    }
}
