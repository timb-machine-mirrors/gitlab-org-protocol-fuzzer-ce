using System;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	class EulaTests
	{
		[Datapoints]
		public string[] versions = Enum.GetNames(typeof(PortableLicense.LicenseFeature));

		[Theory]
		public void HaveEulaText(string version)
		{
			PortableLicense.LicenseFeature ver;
 			if (!Enum.TryParse(version, out ver))
				Assert.Fail("Enumeration value '{0}' is not a valid License.Version".Fmt(version));

			var license = new PortableLicense();
			var txt = license.EulaText;

			Assert.NotNull(txt);
			Assert.Greater(txt.Length, 0);
		}
	}
}
