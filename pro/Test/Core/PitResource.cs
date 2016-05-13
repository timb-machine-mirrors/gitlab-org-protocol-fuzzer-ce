using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Peach.Pro.Test.Core
{
	public class PitManifest
	{
		public Dictionary<string, PitManifestFeature> Features { get; set; }
	}

	public class PitManifestFeature
	{
		public string Pit { get; set; }
		public string[] Assets { get; set; }
		public byte[] Secret { get; set; }
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
			return string.Join(".", parts);
		}

		private static PitManifest LoadManifest(Stream stream)
		{
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				return CreateSerializer().Deserialize<PitManifest>(json);
			}
		}

		private static void SaveManifest(Stream stream, PitManifest manifest)
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
				var password = MakePassword(feature.Key, masterSalt);
				master.Features[feature.Key] = new PitManifestFeature
				{
					Secret = password
				};
				EncryptFeature(asm, prefix, feature, module, password);
			}

			var ms = new MemoryStream();
			SaveManifest(ms, manifest);
			var manifestName = MakeFullName(prefix, ManifestName);
			module.DefineManifestResource(manifestName, ms, ResourceAttributes.Public);

			builder.Save(fileName);

			return master;
		}

		static byte[] MakePassword(string feature, string salt)
		{
			using (var algorithm = SHA256.Create())
			{
				using (var crypto = new CryptoStream(Stream.Null, algorithm, CryptoStreamMode.Write))
				using (var writer = new StreamWriter(crypto))
				{
					var password = salt + feature + salt;
					writer.Write(password);
				}
				return algorithm.Hash;
			}
		}

		static void EncryptFeature(
			Assembly asm,
			string prefix,
			KeyValuePair<string, PitManifestFeature> feature,
			ModuleBuilder module,
			byte[] password)
		{
			using (var cipher = Aes.Create())
			{
				cipher.Mode = CipherMode.CBC;
				cipher.Padding = PaddingMode.PKCS7;
				cipher.GenerateIV();
				feature.Value.Secret = cipher.IV;

				using (var pbkdf = new Rfc2898DeriveBytes(password, feature.Value.Secret, 1000))
				{
					cipher.Key = pbkdf.GetBytes(cipher.KeySize / 8);
				}

				using (var encrypter = cipher.CreateEncryptor())
				{
					foreach (var asset in feature.Value.Assets)
					{
						var rawAssetName = feature.Key + "." + asset;
						var inputResourceName = MakeFullName(prefix, asset);
						var outputResourceName = MakeFullName(prefix, rawAssetName);

						var hmac = new HMACSHA256(cipher.Key);

						var output = new MemoryStream();
						output.Seek(hmac.HashSize / 8, SeekOrigin.Begin);

						using (var input = asm.GetManifestResourceStream(inputResourceName))
						using (var csEncrypter = new CryptoStream(input, encrypter, CryptoStreamMode.Read))
						using (var csHasher = new CryptoStream(csEncrypter, hmac, CryptoStreamMode.Read))
						{
							csHasher.CopyTo(output);
						}

						output.Seek(0, SeekOrigin.Begin);
						output.Write(hmac.Hash, 0, hmac.HashSize / 8);

						output.Seek(0, SeekOrigin.Begin);
						module.DefineManifestResource(outputResourceName, output, ResourceAttributes.Public);
					}
				}
			}
		}

		public static Stream DecryptResource(
			Assembly asm,
			string prefix,
			string featureName,
			PitManifestFeature feature,
			string asset,
			byte[] password)
		{
			using (var cipher = Aes.Create())
			{
				cipher.Mode = CipherMode.CBC;
				cipher.Padding = PaddingMode.PKCS7;
				cipher.IV = feature.Secret;

				using (var pbkdf = new Rfc2898DeriveBytes(password, feature.Secret, 1000))
				{
					cipher.Key = pbkdf.GetBytes(cipher.KeySize / 8);
				}

				var decrypter = cipher.CreateDecryptor();
				var rawAssetName = featureName + "." + asset;
				var resourceName = MakeFullName(prefix, rawAssetName);

				var hmac = new HMACSHA256(cipher.Key);
				var hash = new byte[hmac.HashSize / 8];

				var output = new MemoryStream();
				var csDecrypter = new CryptoStream(output, decrypter, CryptoStreamMode.Write);

				using (var stream = asm.GetManifestResourceStream(resourceName))
				using (var csHasher = new CryptoStream(stream, hmac, CryptoStreamMode.Read))
				{
					stream.Read(hash, 0, hmac.HashSize / 8);
					csHasher.CopyTo(csDecrypter);
				}

				for (var i = 0; i < hmac.HashSize / 8; i++)
				{
					var computed = hmac.Hash;
					if (hash[i] != computed[i])
						return null;
				}

				csDecrypter.FlushFinalBlock();

				output.Seek(0, SeekOrigin.Begin);
				return output;
			}
		}
	}
}

