using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	public class LicenseTests
	{
		[Test]
		public void TestBasic()
		{
			var manifest = new PitManifest
			{
				Features = new Dictionary<string, PitManifestFeature>
				{
					{
						"Foo",
						new PitManifestFeature
						{
							Pit = "Category/Foo",
						}
					}
				},
				Packs = new Dictionary<string, string[]>
				{
					{
						"Professional",
						new string[] 
						{
							"Foo",
						}
					}
				}
			};

			var secrets = new PitManifest
			{
				Features = new Dictionary<string, PitManifestFeature>
				{
					{
						"Foo",
						new PitManifestFeature
						{
							Key = Encoding.UTF8.GetBytes("secret"),
						}
					}
				}
			};

			ILicense license = new PortableLicense(manifest, secrets);
			Console.WriteLine(license.Features.ToArray());
		}
	}
}

