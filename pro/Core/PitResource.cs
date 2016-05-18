using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.IO;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Peach.Core.IO;

namespace Peach.Pro.Core
{
	public class PitManifest
	{
		public Dictionary<string, PitManifestFeature> Features { get; set; }
	}

	public class PitManifestFeature
	{
		public string Pit { get; set; }
		public byte[] Key { get; set; }
		public string[] Assets { get; set; }
	}

	public static class PitResourceLoader
	{
		static readonly string ManifestName = "manifest.json";

		internal static JsonSerializer CreateSerializer()
		{
			var settings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore,
			};
			return JsonSerializer.Create(settings);
		}

		public static PitManifest LoadManifest(Assembly asm, string prefix)
		{
			var name = MakeFullName(prefix, ManifestName);
			using (var stream = asm.GetManifestResourceStream(name))
				return LoadManifest(stream);
		}

		internal static string MakeFullName(string prefix, string name)
		{
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(prefix))
				parts.Add(prefix);
			parts.Add(name);
			return string.Join(".", parts)
				.Replace("/", ".")
				.Replace("\\", ".");
		}

		private static PitManifest LoadManifest(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				return CreateSerializer().Deserialize<PitManifest>(json);
			}
		}

		public static void SaveManifest(Stream stream, PitManifest manifest)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true))
			{
				CreateSerializer().Serialize(writer, manifest);
			}
		}

		public static PitManifest EncryptResources(
			Assembly asm,
			string prefix,
			string output,
			string masterSalt)
		{
			var dir = Path.GetDirectoryName(output);
			var asmName = Path.GetFileNameWithoutExtension(output);
			var fileName = Path.GetFileName(output);

			var builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(asmName),
				AssemblyBuilderAccess.Save,
				dir
			);
			var module = builder.DefineDynamicModule(asmName, fileName);

			var master = new PitManifest
			{
				Features = new Dictionary<string, PitManifestFeature>()
			};

			var manifest = LoadManifest(asm, prefix);
			foreach (var feature in manifest.Features)
			{
				var key = MakeKey(feature.Key, masterSalt);
				master.Features[feature.Key] = new PitManifestFeature
				{
					Key = key
				};

				EncryptFeature(asm, prefix, feature, module, key);
			}

			var ms = new MemoryStream();
			SaveManifest(ms, manifest);
			var manifestName = MakeFullName(prefix, ManifestName);
			module.DefineManifestResource(manifestName, ms, ResourceAttributes.Public);

			builder.Save(fileName);

			return master;
		}

		static void EncryptFeature(
			Assembly asm,
			string prefix,
			KeyValuePair<string, PitManifestFeature> feature,
			ModuleBuilder module,
			byte[] key)
		{
			foreach (var asset in feature.Value.Assets)
			{
				var cipher = MakeCipher(asset, key, true);

				var rawAssetName = feature.Key + "." + asset;
				var inputResourceName = MakeFullName(prefix, asset);
				var outputResourceName = MakeFullName(prefix, rawAssetName);
				Console.WriteLine("{0} -> {1}", inputResourceName, outputResourceName);

				var output = new MemoryStream();
				using (var input = asm.GetManifestResourceStream(inputResourceName))
				using (var wrapper = new NonClosingStreamWrapper(output))
				using (var encrypter = new CipherStream(wrapper, null, cipher))
				{
					input.CopyTo(encrypter);
				}

				output.Seek(0, SeekOrigin.Begin);
				module.DefineManifestResource(outputResourceName, output, ResourceAttributes.Public);
			}
		}

		public static Stream DecryptResource(
			Assembly asm,
			string prefix,
			KeyValuePair<string, PitManifestFeature> feature,
			string asset,
			byte[] key)
		{
			var cipher = MakeCipher(asset, key, false);

			var rawAssetName = feature.Key + "." + asset;
			var resourceName = MakeFullName(prefix, rawAssetName);

			var output = new MemoryStream();
			using (var input = asm.GetManifestResourceStream(resourceName))
			using (var wrapper = new NonClosingStreamWrapper(input))
			using (var decrypter = new CipherStream(wrapper, cipher, null))
			{
				try
				{
					decrypter.CopyTo(output);
				}
				catch (InvalidCipherTextException)
				{
					// MAC check for GCM failed
					return null;
				}
			}

			output.Seek(0, SeekOrigin.Begin);
			return output;
		}

		static byte[] MakeKey(string feature, string salt)
		{
			var saltBytes = Encoding.UTF8.GetBytes(salt);
			var password = Encoding.UTF8.GetBytes(feature);

			var digest = new Sha256Digest();
			var key = new byte[digest.GetDigestSize()];

			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.BlockUpdate(password, 0, password.Length);
			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.DoFinal(key, 0);

			return key;
		}

		static byte[] Digest(byte[] input)
		{
			var digest = new Sha256Digest();
			var output = new byte[digest.GetDigestSize()];
			digest.BlockUpdate(input, 0, input.Length);
			digest.DoFinal(output, 0);
			return output;
		}

		static IBufferedCipher MakeCipher(string asset, byte[] key, bool forEncryption)
		{
			var iv = Digest(Encoding.UTF8.GetBytes(asset));
			var keyParam = new KeyParameter(key);
			var cipherParams = new AeadParameters(keyParam, 16 * 8, iv);
			var blockCipher = new AesFastEngine();
			var aeadBlockCipher = new GcmBlockCipher(blockCipher);
			var cipher = new BufferedAeadBlockCipher(aeadBlockCipher);
			cipher.Init(forEncryption, cipherParams);
			return cipher;
		}
	}
}
