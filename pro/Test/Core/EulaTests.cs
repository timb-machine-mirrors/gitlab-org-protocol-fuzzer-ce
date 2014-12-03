using System;
using NUnit.Framework;
using Peach.Core;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	class EulaTests
	{
		[Datapoints]
		public string[] versions = Enum.GetNames(typeof(License.Feature));

		[Theory]
		public void HaveEulaText(string version)
		{
			License.Feature ver;
 			if (!Enum.TryParse<License.Feature>(version, out ver))
				Assert.Fail("Enumeration value '{0}' is not a valid License.Version".Fmt(version));

			var txt = License.EulaText(ver);

			Assert.NotNull(txt);
			Assert.Greater(txt.Length, 0);
		}
	}
}
