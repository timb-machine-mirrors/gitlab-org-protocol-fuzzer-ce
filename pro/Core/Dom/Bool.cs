using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using NLog;

using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;


namespace Peach.Enterprise.Dom
{
    [DataElement("Bool")]
    [PitParsable("Bool")]
    [Parameter("name", typeof(string), "Name of element", "")]
    [Parameter("mutable", typeof(bool), "Is element mutable", "true")]
    [Parameter("value", typeof(string), "Default value", "")]
    [Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
    [Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
    [Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
    [Parameter("occurs", typeof(int), "Actual occurances", "1")]
    [Serializable]
    public class Bool : Number
    {
        protected static NLog.Logger logger = LogManager.GetCurrentClassLogger();

        public Bool()
        {
            this.lengthType = LengthType.Bits;
            this.length = 1;
        }

        public Bool(string name)
            : base(name)
        {
            this.lengthType = LengthType.Bits;
            this.length = 1;
        }

        public static new DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
        {
            if (node.Name != "Bool")
                return null;

            var elem = DataElement.Generate<Bool>(node, parent);

            context.handleCommonDataElementAttributes(node, elem);

            return elem;
        }

        public override void WritePit(XmlWriter pit)
        {
            pit.WriteStartElement("Bool");

            pit.WriteAttributeString("name", name);
            
            WritePitCommonAttributes(pit);
            WritePitCommonValue(pit);

            pit.WriteEndElement();
        }

        protected override Variant GenerateInternalValue()
        {
            return base.GenerateInternalValue();
        }

        protected override BitwiseStream InternalValueToBitStream()
        {
            return new BitStream();
        }
    }
}

