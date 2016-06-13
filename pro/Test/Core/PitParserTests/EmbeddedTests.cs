﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;

namespace Peach.Pro.Test.Core.PitParserTests
{
	[TestFixture]
	[Quick]
	[Peach]
	public class EmbeddedTests
	{
		static readonly ResourceRoot ResourceRoot = new ResourceRoot
		{
			Assembly = Assembly.GetExecutingAssembly(),
			Prefix = "Peach.Pro.Test.Core.Resources.Pits"
		};
		const string MasterSalt = "salt";

		TempDirectory _tmpDir;

		[SetUp]
		public void SetUp()
		{
			_tmpDir = new TempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			_tmpDir.Dispose();
		}

		[Test]
		public void TestUnlicensedPit()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractFile(_tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var encrypted = Path.Combine(_tmpDir.Path, "Peach.Pro.Pits.dll");

			PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var license = new Mock<ILicense>();
			license.Setup(x => x.GetFeature(featureName))
				   .Returns(() => null);

			var encryptedRoot = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};

			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");

			Assert.That(() =>
			{
				new ProPitParser(
					license.Object,
					_tmpDir.Path,
					pitFile,
					encryptedRoot
				);
			},
				Throws.TypeOf<PeachException>()
					.With.Message.Contains("Your license does not include support for 'Net/DNP3_Slave.xml'.")
			);
		}

		[Test]
		public void TestLoadFromAssembly()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractFile(_tmpDir.Path, "DNP3.py", "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var encrypted = Path.Combine(_tmpDir.Path, "Peach.Pro.Pits.dll");

			var master = PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var feature = new Mock<IFeature>();
			feature.SetupGet(x => x.Key)
				   .Returns(master.Features[featureName].Key);

			var license = new Mock<ILicense>();
			license.Setup(x => x.GetFeature(featureName))
				   .Returns(feature.Object);

			var encryptedRoot = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};

			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");
			var pitConfigFile = pitFile + ".config";

			var defs = PitDefines.ParseFile(pitConfigFile, _tmpDir.Path, new Dictionary<string, string> {
				{"Source", "0"},
				{"Destination", "0"},
			});

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defs.Evaluate() }
			};

			var parser = new ProPitParser(
				license.Object,
				_tmpDir.Path,
				pitFile,
				encryptedRoot
			);
			var dom = parser.asParser(args, pitFile);
			var config = new RunConfiguration() { singleIteration = true, };
			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}

		[Test]
		public void TestLoadFromDisk()
		{
			var license = new Mock<ILicense>();

			ExtractDirectory(_tmpDir.Path, "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Models", "Net");
			ExtractDirectory(_tmpDir.Path, "_Common", "Samples", "Net", "DNP3");

			var pitFile = Path.Combine(_tmpDir.Path, "Net", "DNP3_Slave.xml");
			var pitConfigFile = pitFile + ".config";

			var defs = PitDefines.ParseFile(pitConfigFile, _tmpDir.Path, new Dictionary<string, string> {
				{"Source", "0"},
				{"Destination", "0"},
			});

			var args = new Dictionary<string, object> {
				{ PitParser.DEFINED_VALUES, defs.Evaluate() }
			};

			var parser = new ProPitParser(license.Object, _tmpDir.Path, pitFile);
			var dom = parser.asParser(args, pitFile);
			var config = new RunConfiguration() { singleIteration = true, };
			var e = new Engine(null);
			e.startFuzzing(dom, config);
		}

		[Test]
		public void ParseManifest()
		{
			var manifest = PitResourceLoader.LoadManifest(ResourceRoot);
			CollectionAssert.Contains(manifest.Features.Keys, "PeachPit-Net-DNP3_Slave");
		}

		[Test]
		public void TestProtectResources()
		{
			var featureName = "PeachPit-Net-DNP3_Slave";
			var otherFeatureName = "PeachPit-Net-DNP3_Master";
			var asset1 = "_Common.Models.Net.DNP3_State.xml";
			var asset2 = "_Common.Models.Net.DNP3_Data.xml";
			var expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
			var encrypted = Path.Combine(_tmpDir.Path, "TestProtectResources.dll");

			var master = PitResourceLoader.EncryptResources(ResourceRoot, encrypted, MasterSalt);

			var root = new ResourceRoot
			{
				Assembly = LoadAssembly(encrypted),
				Prefix = ResourceRoot.Prefix
			};
			var manifest = PitResourceLoader.LoadManifest(root);

			{
				var actual = GetFirstLine(root.Assembly, featureName, asset1);
				Assert.AreNotEqual(expected, actual);
			}

			{
				var actual = GetFirstLine(root.Assembly, featureName, asset2);
				Assert.AreNotEqual(expected, actual);
			}

			var feature = manifest.Features[featureName];
			using (var stream = PitResourceLoader.DecryptResource(
				root,
				new KeyValuePair<string, PitManifestFeature>(featureName, feature),
				asset1,
				master.Features[featureName].Key))
			using (var reader = new StreamReader(stream))
			{
				var actual = reader.ReadLine();
				Assert.AreEqual(expected, actual);
			}

			using (var stream = PitResourceLoader.DecryptResource(
				root,
				new KeyValuePair<string, PitManifestFeature>(featureName, feature),
				asset2,
				master.Features[featureName].Key))
			using (var reader = new StreamReader(stream))
			{
				var actual = reader.ReadLine();
				Assert.AreEqual(expected, actual);
			}

			// other features use different passwords
			// this should fail since we are using the wrong password
			var otherFeature = manifest.Features[otherFeatureName];
			using (var stream = PitResourceLoader.DecryptResource(
				root,
				new KeyValuePair<string, PitManifestFeature>(otherFeatureName, otherFeature),
				asset1,
				master.Features[featureName].Key))
			{
				Assert.IsNull(stream);
			}
		}

		private string GetFirstLine(Assembly asm, string featureName, string asset)
		{
			var rawAssetName = featureName + "." + asset;

			var name = PitResourceLoader.MakeFullName(ResourceRoot.Prefix, rawAssetName);
			using (var stream = asm.GetManifestResourceStream(name))
			using (var reader = new StreamReader(stream))
			{
				return reader.ReadLine();
			}
		}

		private static Assembly LoadAssembly(string encrypted)
		{
			using (var ms = new MemoryStream())
			using (var fs = File.OpenRead(encrypted))
			{
				fs.CopyTo(ms);
				return Assembly.Load(ms.ToArray());
			}
		}

		private static void ExtractDirectory(string targetDir, params string[] parts)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(parts));
			Directory.CreateDirectory(dir);

			var prefix = string.Join(".",
				new[] { ResourceRoot.Prefix }
				.Concat(parts)
			);
			foreach (var name in ResourceRoot.Assembly.GetManifestResourceNames())
			{
				if (name.StartsWith(prefix))
				{
					var fileName = name.Substring(prefix.Length + 1); // exclude last '.'
					var target = Path.Combine(dir, fileName);
					Utilities.ExtractEmbeddedResource(ResourceRoot.Assembly, name, target);
				}
			}
		}

		private static void ExtractFile(string targetDir, string filename, params string[] dirs)
		{
			var sep = new string(new[] { Path.DirectorySeparatorChar });
			var dir = string.Join(sep, new[] { targetDir }.Concat(dirs));
			Directory.CreateDirectory(dir);

			var name = string.Join(".",
				new[] { ResourceRoot.Prefix }
				.Concat(dirs)
				.Concat(new[] { filename })
			);
			var target = Path.Combine(dir, filename);
			Utilities.ExtractEmbeddedResource(ResourceRoot.Assembly, name, target);
		}
	}
}