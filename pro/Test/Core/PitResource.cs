using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Peach.Pro.Core;

namespace Peach.Pro.Test.Core
{
	public class PitManifest
	{
		public byte[] Salt { get; set; }
		public byte[] IV { get; set; }
		public Dictionary<string, PitManifestFeature> Features { get; set; }
	}

	public class PitManifestFeature
	{
		public string Pit { get; set; }
		public string[] Assets { get; set; }
	}

	public static class PitResourceLoader
	{
		public static PitManifest LoadManifest(Assembly asm, string prefix)
		{
			var name = MakeFullName(prefix, "manifest.json");
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
				return JsonUtilities.CreateSerializer().Deserialize<PitManifest>(json);
			}
		}

		private static void SaveManifest(Stream stream, PitManifest manifest)
		{
			using (var writer = new StreamWriter(stream, Encoding.UTF8, 4096, true))
			{
				JsonUtilities.CreateSerializer().Serialize(writer, manifest);
			}
		}

		public static void EncryptResources(Assembly asmInput, string output, string password)
		{
			var dir = Path.GetDirectoryName(output);
			var asmName = Path.GetFileNameWithoutExtension(output);
			var fileName = Path.GetFileName(output);

			var builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				new AssemblyName(asmName),
				System.Reflection.Emit.AssemblyBuilderAccess.Save,
				dir
			);

			using (var cipher = new AesManaged())
			{
				cipher.Mode = CipherMode.CBC;
				cipher.Padding = PaddingMode.PKCS7;
				cipher.GenerateIV();

				byte[] salt;
				using (var pbkdf = new Rfc2898DeriveBytes(password, cipher.KeySize / 8))
				{
					salt = pbkdf.Salt;
					cipher.Key = pbkdf.GetBytes(cipher.KeySize / 8);
				}

				using (var encrypter = cipher.CreateEncryptor())
				{
					var module = builder.DefineDynamicModule(asmName, fileName);
					foreach (var resourceName in asmInput.GetManifestResourceNames())
					{
						if (resourceName.Contains("manifest.json"))
						{
							PitManifest manifest;
							using (var stream = asmInput.GetManifestResourceStream(resourceName))
							{
								manifest = LoadManifest(stream);
								manifest.IV = cipher.IV;
								manifest.Salt = salt;
							}

							var ms = new MemoryStream();
							SaveManifest(ms, manifest);
							module.DefineManifestResource(resourceName, ms, ResourceAttributes.Public);
						}
						else
						{
							var ms = new MemoryStream();
							using (var stream = asmInput.GetManifestResourceStream(resourceName))
							using (var crypto = new CryptoStream(stream, encrypter, CryptoStreamMode.Read))
							{
								crypto.CopyTo(ms);
								crypto.Clear();
							}

							ms.Seek(0, SeekOrigin.Begin);
							module.DefineManifestResource(resourceName, ms, ResourceAttributes.Public);
						}
					}
					cipher.Clear();
				}

				builder.Save(fileName);
			}
		}

		public static Stream DecryptResource(Assembly asm, string prefix, string resourceName, string password)
		{
			var manifest = LoadManifest(asm, prefix);

			var cipher = new AesManaged();
			cipher.Mode = CipherMode.CBC;
			cipher.Padding = PaddingMode.PKCS7;
			cipher.IV = manifest.IV;

			using (var pbkdf = new Rfc2898DeriveBytes(password, manifest.Salt))
			{
				cipher.Key = pbkdf.GetBytes(cipher.KeySize / 8);
			}

			var decrypter = cipher.CreateDecryptor();
			var name = MakeFullName(prefix, resourceName);

			var ms = new MemoryStream();
			using (var stream = asm.GetManifestResourceStream(name))
			using (var crypto = new CryptoStream(stream, decrypter, CryptoStreamMode.Read))
			{
				crypto.CopyTo(ms);
				crypto.Clear();
			}

			ms.Seek(0, SeekOrigin.Begin);
			return ms;
		}
	}
}

