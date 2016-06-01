using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Portable.Licensing;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.Core
{
	[TestFixture]
	[Quick]
	[Peach]
	public class LicenseTests
	{
		const string PrivateKey = "MIICITAjBgoqhkiG9w0BDAEDMBUEEC9fOLhxBBNFcBQZRKva2NECAQoEggH49lHJvl5mf49zR1RVxoJGcaA4OYVCWn9Vg+zXUhBRU0IJP+SqBvejus8xL5LmUApSc/2MYaHmonKEwFL/d6D1hpiPDzoDORLX+Ftwwo0a3ZJX4YhmOOb20ySmVhywOIrdgMi9IyzCvTGs8mMK++8pArwUParudbxF8gz6vIXHyTaVwmizVvr8NjFycJDuobG07BsryPHfw8r986KKyqsLQ29lYfKPlkF9MuroRK+sUkhTuJPQIVs3Fkn8udladAQxH63zRGfV1PblvS0C8hgHEOk9ML31RP3XU3Mf64Qo7irTZBy+Qd20QtMISRF1zQWTe66cAvVRn8pZWs7IUdZQZzsJJKRIPeaqsmlQUu+FDCrA72ac5TpuK/fzeFP69kTMpvL3GowhPOauDz66KwZCXoGi9oEuEWini+0DG8QXbCvCIlQEmQttWrESgYztnJHrU2yjq6isA6cQqzMR2UzWlab7yZBOWV590Hg76SJPw7k3BujIqO+irmyPTOXQzQ5uVuKc5n7gfjoGXI/ThXBj7TaF65IRSIqsSPNnBORZffm1VJ9MpydlYUDEG6VTn08zpRB9d1dmeGS2si8FqSupECm05GjayZXrbA7J9eH0VtKZ0wLSAvxdOGtcAm3OBCec5TLHWYVPGizzjZSs8utlyVidI1ljtPFL";
		const string PassPhrase = "jGJ+.bACcSy7";

		Assembly _asm = Assembly.GetExecutingAssembly();
		const string Prefix = "Peach.Pro.Test.Core.Resources.Licenses";

		readonly PitManifest _manifest;
		readonly PitManifest _secrets;

		public LicenseTests()
		{
			_manifest = PitResourceLoader.LoadManifest(new ResourceRoot
			{
				Assembly = _asm,
				Prefix = Prefix,
			});

			_secrets = new PitManifest
			{
				Features = new Dictionary<string, PitManifestFeature>()
			};

			foreach (var kv in _manifest.Features)
			{
				_secrets.Features[kv.Key] = new PitManifestFeature
				{
					Key = Encoding.UTF8.GetBytes("secret")
				};
			}
		}

		[Test]
		public void TestTypical()
		{
			var lic = License.New()
				.WithProductFeatures(new Dictionary<string, string>
				{
					{"Developer", ""},
					{"pit:HTTP", ""},
					{"pit:PNG", ""},
					{"Studio", ""},
				})
				.As(LicenseType.Standard)
				.ExpiresAt(DateTime.UtcNow + TimeSpan.FromDays(2))
				.CreateAndSignWithPrivateKey(PrivateKey, PassPhrase);

			DoTest("Typical.license", lic, new[]
			{
				FeatureNames.Android,
				FeatureNames.Engine,
				FeatureNames.ExportPit,
				FeatureNames.CustomPit,

				// pit:HTTP
				"PeachPit-Net-HTTP_Client_SSL",
				"PeachPit-Net-HTTP_Server",
				"PeachPit-Net-HTTP_Server_SSL",
				"PeachPit-Net-HTTP_Client",
	
				// pit:PNG
				"PeachPit-Image-PNG",
			});
		}

		[Test]
		public void TestTrial()
		{
			var lic = License.New()
				.WithProductFeatures(new Dictionary<string, string>
				{
					{"pit:DICOM_File", ""},
					{"pit:DICOM_Net", ""},
					{"Trial", ""},
				})
				.As(LicenseType.Trial)
				.ExpiresAt(DateTime.UtcNow + TimeSpan.FromDays(2))
				.CreateAndSignWithPrivateKey(PrivateKey, PassPhrase);

			DoTest("Trial.license", lic, new[]
			{
				FeatureNames.Android,
				FeatureNames.Engine,
				FeatureNames.ExportPit,
				FeatureNames.CustomPit,

				// pit:DICOM_File
				"PeachPit-Image-DICOM_File",
	
				// pit:DICOM_Net
				"PeachPit-Net-DICOM_Net_Provider",
				"PeachPit-Net-DICOM_Net_User",

				// Trial
				"PeachPit-Net-HTTP_Client_SSL",
				"PeachPit-Net-HTTP_Server",
				"PeachPit-Net-HTTP_Server_SSL",
				"PeachPit-Net-HTTP_Client",
				"PeachPit-Image-PNG",
			});
		}

		[Test]
		public void TestDevPack()
		{
			var lic = License.New()
				.WithProductFeatures(new Dictionary<string, string>
				{
					{"Developer", ""},
					{"pack:NetworkServices", ""},
					{"pack:WebProtocol", ""},
					{"Studio", ""},
				})
				.As(LicenseType.Standard)
				.ExpiresAt(DateTime.UtcNow + TimeSpan.FromDays(2))
				.CreateAndSignWithPrivateKey(PrivateKey, PassPhrase);

			DoTest("DevPack.license", lic, new[]
			{
				FeatureNames.Android,
				FeatureNames.Engine,
				FeatureNames.ExportPit,
				FeatureNames.CustomPit,

				// pack:NetworkServices
				"PeachPit-Net-DNS_Client_TCP",
				"PeachPit-Net-DNS_Client_UDP",
				"PeachPit-Net-DNS_Server_TCP",
				"PeachPit-Net-DNS_Server_UDP",
				"PeachPit-Net-FTP_Client_Active",
				"PeachPit-Net-FTP_Client_Passive",
				"PeachPit-Net-FTP_Server_Active",
				"PeachPit-Net-FTP_Server_Passive",
				"PeachPit-Net-NTP_Server",
				"PeachPit-Net-TELNET_Client",
				"PeachPit-Net-TELNET_Server",
				"PeachPit-Net-POP3_Server",
				"PeachPit-Net-SSH-FTP_Server",

				// pack:WebProtocol
				"PeachPit-Net-HTTP_Client_SSL",
				"PeachPit-Net-HTTP_Server",
				"PeachPit-Net-HTTP_Server_SSL",
				"PeachPit-Net-HTTP_Client",
				"PeachPit-Net-SSL_TLS-1.0_Client",
				"PeachPit-Net-SSL_TLS-1.0_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.0_Server",
				"PeachPit-Net-SSL_TLS-1.1_Client",
				"PeachPit-Net-SSL_TLS-1.1_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.1_Server",
				"PeachPit-Net-SSL_TLS-1.2_Client",
				"PeachPit-Net-SSL_TLS-1.2_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.2_Server",
			});
		}

		[Test]
		public void TestPitsPacks()
		{
			var lic = License.New()
				.WithProductFeatures(new Dictionary<string, string>
				{
					{"Developer", ""},
					{"pack:WebProtocol", ""},
					{"pit:DICOM_File", ""},
					{"Studio", ""},
				})
				.As(LicenseType.Standard)
				.ExpiresAt(DateTime.UtcNow + TimeSpan.FromDays(2))
				.CreateAndSignWithPrivateKey(PrivateKey, PassPhrase);

			DoTest("PitsPacks.license", lic, new[]
			{
				FeatureNames.Android,
				FeatureNames.Engine,
				FeatureNames.ExportPit,
				FeatureNames.CustomPit,

				// pit:DICOM_File
				"PeachPit-Image-DICOM_File",

				// pack:WebProtocol
				"PeachPit-Net-HTTP_Client_SSL",
				"PeachPit-Net-HTTP_Server",
				"PeachPit-Net-HTTP_Server_SSL",
				"PeachPit-Net-HTTP_Client",
				"PeachPit-Net-SSL_TLS-1.0_Client",
				"PeachPit-Net-SSL_TLS-1.0_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.0_Server",
				"PeachPit-Net-SSL_TLS-1.1_Client",
				"PeachPit-Net-SSL_TLS-1.1_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.1_Server",
				"PeachPit-Net-SSL_TLS-1.2_Client",
				"PeachPit-Net-SSL_TLS-1.2_Client_Verify",
				"PeachPit-Net-SSL_TLS-1.2_Server",
			});
		}

		void DoTest(string name, License lic, string[] expected)
		{
			var buf = new StringWriter();
			lic.Save(buf);
			var xml = buf.ToString();

			var licFile = new PortableLicense.LicFile
			{
				FileName = name,
				Contents = xml
			};

			ILicense license = new PortableLicense(_manifest, _secrets, licFile);
			CollectionAssert.AreEquivalent(expected, license.Features.Select(x => x.Name));

			foreach (var item in expected)
			{
				Assert.IsNotNull(license.GetFeature(item));
			}

			Assert.IsNull(license.GetFeature("Bar"));
		}
	}
}

