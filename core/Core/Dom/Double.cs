﻿using System;
using System.Collections.Generic;
using System.Globalization;
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


namespace Peach.Core.Dom
{
    [DataElement("Double")]
    [PitParsable("Double")]
    [Parameter("name", typeof(string), "Name of element", "")]
    [Parameter("size", typeof(uint), "Size in bits")]
    [Parameter("endian", typeof(EndianType), "Byte order of number", "little")]
    [Parameter("mutable", typeof(bool), "Is element mutable", "true")]
    [Parameter("valueType", typeof(Peach.Core.Dom.ValueType), "Format of value attribute", "string")]
    [Parameter("value", typeof(string), "Default value", "")]
    [Parameter("constraint", typeof(string), "Scripting expression that evaluates to true or false", "")]
    [Parameter("minOccurs", typeof(int), "Minimum occurances", "1")]
    [Parameter("maxOccurs", typeof(int), "Maximum occurances", "1")]
    [Parameter("occurs", typeof(int), "Actual occurances", "1")]
    [Serializable]
    public class Double : DataElement
    {
        protected double _max = double.MaxValue;
        protected double _min = double.MinValue;
        protected bool _isLittleEndian = true;
        protected Endian _endian = Endian.Little;

        public Double()
            : base()
        {
            lengthType = LengthType.Bits;
            length = 64;
            DefaultValue = new Variant(0.0);
        }

        public Double(string name)
            : base(name)
        {
            lengthType = LengthType.Bits;
            length = 64;
            DefaultValue = new Variant(0.0);
        }

        public static DataElement PitParser(PitParser context, XmlNode node, DataElementContainer parent)
        {
            if (node.Name != "Double")
                return null;
            
            var num = DataElement.Generate<Double>(node, parent);

            if (node.hasAttr("size"))
            {
                int size = node.getAttrInt("size");

                if (size != 32 && size != 64)
                    throw new PeachException(string.Format("Error, unsupported size '{0}' for {1}.", size, num.debugName));

                num.lengthType = LengthType.Bits;
                num.length = size;
            }

            string strEndian = null;
            if (node.hasAttr("endian"))
                strEndian = node.getAttrString("endian");
            if (strEndian == null)
                strEndian = context.getDefaultAttr(typeof(Double), "endian", null);

            if (strEndian != null)
            {
                switch (strEndian.ToLower())
                {
                    case "little":
                        num.LittleEndian = true;
                        break;
                    case "big":
                        num.LittleEndian = false;
                        break;
                    case "network":
                        num.LittleEndian = false;
                        break;
                    default:
                        throw new PeachException(
                            string.Format("Error, unsupported value '{0}' for 'endian' attribute on {1}.", strEndian, num.debugName));
                }
            }

            context.handleCommonDataElementAttributes(node, num);
            context.handleCommonDataElementChildren(node, num);
            context.handleCommonDataElementValue(node, num);

            return num;
        }

        public override void WritePit(XmlWriter pit)
        {
            pit.WriteStartElement("Double");

            pit.WriteAttributeString("size", lengthAsBits.ToString());

            if (!LittleEndian)
                pit.WriteAttributeString("endian", "big");

            WritePitCommonAttributes(pit);
            WritePitCommonValue(pit);
            WritePitCommonChildren(pit);

            pit.WriteEndElement();
        }

        public override long length
        {
            get
            {
                switch (_lengthType)
                {
                    case LengthType.Bytes:
                        return _length;
                    case LengthType.Bits:
                        return _length;
                    case LengthType.Chars:
                        throw new NotSupportedException("Length type of Chars not supported by Number.");
                    default:
                        throw new NotSupportedException("Error calculating length.");
                }
            }
            set
            {
                if (value != 32 &&  value != 64)
                    throw new ArgumentOutOfRangeException("value", value, "Value must be equal to 32 or 64.");

                if (value == 32)
                {
                    _min = float.MinValue;
                    _max = float.MaxValue;
                }
                else
                {
                    _min = double.MinValue;
                    _max = double.MaxValue;
                }


                base.length = value;

                Invalidate();
            }
        }

        public override bool hasLength
        {
            get
            {
                return true;
            }
        }

        public override Variant DefaultValue
        {
            get
            {
                return base.DefaultValue;
            }
            set
            {
                base.DefaultValue = Sanitize(value);
            }
        }

        #region Sanitize

        private double SanitizeString(string str)
        {
            string conv = str;
            NumberStyles style = NumberStyles.AllowLeadingSign;

            if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                conv = str.Substring(2);
                style = NumberStyles.AllowHexSpecifier;

                ulong val;
                if (ulong.TryParse(conv, style, CultureInfo.InvariantCulture, out val))
                    return (double)val;

                throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit double.", debugName, str, lengthAsBits));
            }

            if (str.Contains("."))
                style = style | NumberStyles.AllowDecimalPoint;

            if (str.Contains("E+") || str.Contains("e+")
                    || str.Contains("E-") || str.Contains("e-"))
                style = style | NumberStyles.AllowExponent;

            double value;
            if (double.TryParse(conv, style, CultureInfo.InvariantCulture, out value))
                return value;

            throw new PeachException(string.Format("Error, {0} value '{1}' could not be converted to a {2}-bit double.", debugName, str, lengthAsBits));
        }

        private double SanitizeStream(BitwiseStream bs)
        {
            if (bs.LengthBits < lengthAsBits || (bs.LengthBits + 7) / 8 != (lengthAsBits + 7) / 8)
                throw new PeachException(string.Format("Error, {0} value has an incorrect length for a {1}-bit double, expected {3} bytes.", debugName, lengthAsBits, (lengthAsBits + 7) / 8));

            return FromBitstream(bs);
        }

        private double FromBitstream(BitwiseStream bs)
        {
            byte[] b = new byte[length / 8];
            int len = bs.Read(b, 0, b.Length);
            System.Diagnostics.Debug.Assert(len == lengthAsBits / 8);

            if (BitConverter.IsLittleEndian != _isLittleEndian)
                System.Array.Reverse(b);

            return BitConverter.ToDouble(b, 0);
        }

        private Variant Sanitize(Variant variant)
        {
            var value = GetNumber(variant);

            if ((double)value < MinValue && (double)value != double.NegativeInfinity)
                throw new PeachException(string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit double.", debugName, value, lengthAsBits));
            if ((double)value > MaxValue && (double)value != double.PositiveInfinity)
                throw new PeachException(string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit double.", debugName, value, lengthAsBits));

            return new Variant((double)value);
        }

        private double GetNumber(Variant variant)
        {
            double value = 0;

            switch (variant.GetVariantType())
            {
                case Variant.VariantType.String:
                    value = SanitizeString((string)variant);
                    break;
                case Variant.VariantType.ByteString:
                    value = SanitizeStream(new BitStream((byte[])variant));
                    break;
                case Variant.VariantType.BitStream:
                    value = SanitizeStream((BitwiseStream)variant);
                    break;
                case Variant.VariantType.Int:
                case Variant.VariantType.Long:
                    value = (double)variant;
                    break;
                case Variant.VariantType.ULong:
                    value = (double)variant;
                    break;
                case Variant.VariantType.Double:
                    value = (double)variant;
                    break;
                default:
                    throw new ArgumentException("Variant type '" + variant.GetVariantType().ToString() + "' is unsupported.", "variant");
            }

            return value;
        }

        #endregion

        public bool LittleEndian
        {
            get { return _isLittleEndian; }
            set
            {
                if (_isLittleEndian != value)
                {
                    _isLittleEndian = value;
                    _endian = value ? Endian.Little : Endian.Big;
                    Invalidate();
                }
            }
        }

        public double MaxValue
        {
            get { return _max; }
        }

        public double MinValue
        {
            get { return _min; }
        }

        protected override BitwiseStream InternalValueToBitStream()
        {
            var value = GetNumber(InternalValue);

            if (value > 0 && (double)value > MaxValue && (double)value != double.PositiveInfinity)
            {
                string msg = string.Format("Error, {0} value '{1}' is greater than the maximum {2}-bit number.", debugName, value, lengthAsBits);
                var inner = new OverflowException(msg);
                throw new SoftException(inner);
            }

            if (value < 0 && (double)value < MinValue && (double)value != double.NegativeInfinity)
            {
                string msg = string.Format("Error, {0} value '{1}' is less than the minimum {2}-bit number.", debugName, value, lengthAsBits);
                var inner = new OverflowException(msg);
                throw new SoftException(inner);
            }

            byte[] b;
            if (length == 32)
                b = BitConverter.GetBytes((float)value);
            else
                b = BitConverter.GetBytes((double)value);

            if (BitConverter.IsLittleEndian != _isLittleEndian)
                System.Array.Reverse(b);


            var bs = new BitStream();
            bs.Write(b, 0, b.Length);
            return bs;
        }
    }
}
