using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Enterprise;

namespace Peach.Enterprise.Test.Dom
{
    class DoubleTests : DataModelCollector
    {
        [Test]
        [Category("Peach")]
        public void SimpleInteralValueTest1()
        {
            var db = new Core.Dom.Double();

            var actual = (double)db.InternalValue;
            var expected = 0.0;
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(64, db.length);
        }

        [Test]
        [Category("Peach")]
        public void SimpleInteralValueTest2()
        {
            var db = new Core.Dom.Double();
            db.length = 32;

            var actual = (double)db.InternalValue;
            var expected = 0.0;
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(32, db.length);
        }

        [Test]
        [Category("Peach")]
        public void SimpleInteralValueTest3()
        {
            var db = new Core.Dom.Double();
            db.length = 32;
            db.DefaultValue = new Core.Variant(1.0E+3);

            var actual = (double)db.InternalValue;
            var expected = 1000.0;
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(32, db.length);
        }

        [Test]
        [Category("Peach")]
        public void SimpleInteralValueTest4()
        {
            var db = new Core.Dom.Double();
            db.length = 64;
            db.LittleEndian = false;
            db.DefaultValue = new Core.Variant(1.0);

            var actual = (double)db.InternalValue;
            var expected = 1.0;
            Assert.AreEqual(expected, actual);
            Assert.AreEqual(64, db.length);
        }

    }
}
