using System;
using System.IO;
using System.Linq;
using System.Reflection;

using Portable.Licensing;
using Portable.Licensing.Validation;

namespace Peach.Core
{
	public static class License
	{
		public enum Feature
		{
			Enterprise,
			Distributed,
			ProfessionalWithConsulting,
			Professional,
			TrialAllPits,
			Trial,
			Acedemic,
			Unknown,
		}

		static License()
		{
			Verify();
		}

		public static bool IsValid { get; private set; }
		public static Feature Version { get; private set; }

		static string Read()
		{
			var fileName = "Peach.license";
			var peachDir = Platform.GetOS() == Platform.OS.Windows ? "Peach" : "peach";
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			var asmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

			try
			{
				// Try common application data first
				// Windows - C:\ProgramData\Peach\Peach.license
				// Unix - /usr/share/peach/Peach.license
				var path = Path.Combine(appData, peachDir, fileName);

				return File.ReadAllText(path);
			}
			catch (FileNotFoundException)
			{
			}
			catch (DirectoryNotFoundException)
			{
			}

			// Look in same place as peach assembly 2nd
			return File.ReadAllText(Path.Combine(asmDir, fileName));
		}

		static void Verify()
		{
			try
			{
				var publicKey = "MIIBKjCB4wYHKoZIzj0CATCB1wIBATAsBgcqhkjOPQEBAiEA/////wAAAAEAAAAAAAAAAAAAAAD///////////////8wWwQg/////wAAAAEAAAAAAAAAAAAAAAD///////////////wEIFrGNdiqOpPns+u9VXaYhrxlHQawzFOw9jvOPD4n0mBLAxUAxJ02CIbnBJNqZnjhE50mt4GffpAEIQNrF9Hy4SxCR/i85uVjpEDydwN9gS3rM6D0oTlF2JjClgIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBA0IABBXgmINzW2CILco6ktkgF2gITUHUoQbu3r9HJSqsBuSGHHrkZ2HWgwZlmkUjTSWrUdZXeTMzhiM3AhlF2ldHvNI=";
				var xml = Read();
				var license = Portable.Licensing.License.Load(xml);

				var failures = license.Validate()
					.ExpirationDate()
					//.When(lic => lic.Type == LicenseType.Trial)
					.And()
					//.AssertThat(lic => lic.ProductFeatures.Contains("Professional"))
					.Signature(publicKey)
					.AssertValidLicense()
					.ToList();

				foreach (var failure in failures)
				{
					Console.WriteLine("{0}  {1}", failure.Message, failure.HowToResolve);
				}

				IsValid = !failures.Any();

				var ver = Enum.GetNames(typeof(Feature)).Where(n => license.ProductFeatures.Contains(n)).FirstOrDefault();
				if (string.IsNullOrEmpty(ver))
					ver = "Unknown";

				Version = (Feature)Enum.Parse(typeof(Feature), ver);

				if (Version == Feature.Unknown)
					throw new NotSupportedException("No supported features were found.");
			}
			catch (Exception ex)
			{
				IsValid = false;
				Version = Feature.Unknown;

				Console.WriteLine("License verification failed.  {0}", ex.Message);
			}
		}
	}
}