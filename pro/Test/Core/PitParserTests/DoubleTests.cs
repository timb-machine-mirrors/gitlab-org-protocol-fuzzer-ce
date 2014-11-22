﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using NUnit.Framework;
using NUnit.Framework.Constraints;

using Peach.Core;
using Peach.Core.Dom;
using Peach.Core.Analyzers;
using Peach.Core.IO;
using Peach.Enterprise.Dom;

namespace Peach.Pro.Test.PitParserTests
{
    [TestFixture]
    [Category("Peach")]
    class DoubleTests
    {
        [Test]
        public void DoubleDefaults()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            Assert.IsTrue(num.LittleEndian);
            Assert.AreEqual(0.0, (double)num.DefaultValue);
        }

        [Test]
        public void DoubleValues()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double name='DoubleMe' size='64' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            Assert.AreEqual(0.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant("100");
            Assert.AreEqual(100.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant("-100");
            Assert.AreEqual(-100.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant("10.0");
            Assert.AreEqual(10.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant("-10.0");
            Assert.AreEqual(-10.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant(0);
            Assert.AreEqual(0, (double)num.DefaultValue);
            
            num.DefaultValue = new Variant(-0);
            Assert.AreEqual(-0, (double)num.DefaultValue);

            num.DefaultValue = new Variant("NaN");
            Assert.AreEqual(double.NaN, (double)num.DefaultValue);

            num.DefaultValue = new Variant("1.0E+100");
            Assert.AreEqual(1.0E+100, (double)num.DefaultValue);

            num.DefaultValue = new Variant("1.0e-100");
            Assert.AreEqual(1.0E-100, (double)num.DefaultValue);

            num.DefaultValue = new Variant("Infinity");
            Assert.AreEqual(double.PositiveInfinity, (double)num.DefaultValue);

            num.DefaultValue = new Variant("-Infinity");
            Assert.AreEqual(double.NegativeInfinity, (double)num.DefaultValue);

            num.DefaultValue = new Variant("0xff");
            Assert.AreEqual(255.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant(-10.0);
            Assert.AreEqual(-10.0, (double)num.DefaultValue);

            num.DefaultValue = new Variant(-10.1);
            Assert.AreEqual(-10.1, (double)num.DefaultValue);
        }

        [Test]
        public void ValueTypeTest()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' valueType='hex' value='0000000000e06f40' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            Assert.IsTrue(num.LittleEndian);
            Assert.AreEqual(255.0, (double)num.DefaultValue);
        }

        [Test]
        public void ValueTest1()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' value='Infinity' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            Assert.IsTrue(num.LittleEndian);
            Assert.AreEqual(double.PositiveInfinity, (double)num.DefaultValue);
        }

        [Test]
        public void ValueTest2()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' value='1' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            Assert.IsTrue(num.LittleEndian);
            Assert.AreEqual(1.0, (double)num.DefaultValue);
        }

        [Test]
        public void ValueTest3()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            var val = num.Value.ToArray();

            var expected = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0xe0, 0x6f, 0x40 };

            Assert.IsTrue(num.LittleEndian);
            Assert.AreEqual(expected, val);
        }

        [Test]
        public void ValueTest4()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' endian='big' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            var val = num.Value.ToArray();

            var expected = new byte[] { 0x40, 0x6f, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x00 };

            Assert.IsTrue(!num.LittleEndian);
            Assert.AreEqual(expected, val);
        }

        [Test, ExpectedException("Peach.Core.PeachException", ExpectedMessage = "Error, Double 'TheDataModel.DataElement_0' value '1.79769313486232E+308' is greater than the maximum 32-bit double.")]
        public void ValueTest5()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='32' endian='big' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            num.DefaultValue = new Variant(double.MaxValue);

            Assert.IsTrue(!num.LittleEndian);
        }

        [Test]
        public void ValueTest6()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='32' endian='big' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            num.DefaultValue = new Variant(float.MaxValue);

            Assert.IsTrue(!num.LittleEndian);
        }

        [Test]
        public void ValueTest7()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='32' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;


            Assert.AreEqual(4, num.Value.ToArray().Length);
            Assert.AreEqual(255.0, (float)((double)num.DefaultValue));
        }

        [Test]
        public void ValueTest8()
        {
            string xml = @"<?xml version='1.0' encoding='utf-8'?>
                <Peach>
                	<DataModel name='TheDataModel'>
                		<Double size='64' endian='big' value='255' />
                	</DataModel>
                </Peach>";

            var parser = new PitParser();
            var dom = parser.asParser(null, new MemoryStream(Peach.Core.ASCIIEncoding.ASCII.GetBytes(xml)));
            var num = dom.dataModels[0][0] as Peach.Core.Dom.Double;

            var val = num.Value.ToArray();

            var expected = new byte[] { 0x40, 0x6f, 0xe0, 0x00, 0x00, 0x00, 0x00, 0x00 };

            Assert.IsTrue(!num.LittleEndian);
            Assert.AreEqual(expected, val);
            Assert.AreEqual(8, val.Length);
        }
    }
}
