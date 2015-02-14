using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.Test;
using Peach.Pro.Core.Dom;

namespace Peach.Pro.Test.Dom
{
	class BoolTests : DataModelCollector
	{
		[Test]
		[Category("Peach")]
		public void SimpleTest()
		{
			var b = new Bool();

			Assert.AreEqual(1, b.lengthAsBits);
			Assert.AreEqual(LengthType.Bits, b.lengthType);
		}
	}
}
