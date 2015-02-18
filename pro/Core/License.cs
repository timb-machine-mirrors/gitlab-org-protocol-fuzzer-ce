using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;

using Portable.Licensing;
using Portable.Licensing.Validation;

namespace Peach.Core
{
	public static class License
	{
		static readonly string EulaConfig = "eulaAccepted";
		static bool? eulaAccepted;
		static object mutex = new object();

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

		static Configuration GetUserConfig()
		{
			var appConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var userFile = new ExeConfigurationFileMap
			{
				ExeConfigFilename = Path.Combine(
					Path.GetDirectoryName(appConfig.FilePath),
					Path.GetFileNameWithoutExtension(appConfig.FilePath)
				) + ".user.config"
			};
			return ConfigurationManager.OpenMappedExeConfiguration(userFile, ConfigurationUserLevel.None);
		}

		static string Get(this KeyValueConfigurationCollection settings, string key)
		{
			var item = settings[key];
			return item != null ? item.Value : string.Empty;
		}

		public static bool EulaAccepted
		{
			get
			{
				lock (mutex)
				{
					if (!eulaAccepted.HasValue)
					{
						var str = GetUserConfig().AppSettings.Settings.Get(EulaConfig);
						bool val;

						eulaAccepted = bool.TryParse(str, out val);
						eulaAccepted &= val;
					}
					return eulaAccepted.Value;
				}
			}
			set
			{
				lock (mutex)
				{
					if (eulaAccepted.HasValue && eulaAccepted.Value == value)
						return;

					var config = GetUserConfig();
					var settings = config.AppSettings.Settings;

					if (settings[EulaConfig] == null)
						settings.Add(EulaConfig, value.ToString());
					else
						settings[EulaConfig].Value = value.ToString();

					config.Save(ConfigurationSaveMode.Modified);
					ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);

					eulaAccepted = value;
				}
			}
		}

		public static string EulaText()
		{
			return EulaText(Version);
		}

		public static string EulaText(Feature version)
		{
			var res = "Peach.Pro.Core.Resources.Eula.{0}.txt".Fmt(version);
			return Utilities.LoadStringResource(Assembly.GetExecutingAssembly(), res);
		}

		static string Read()
		{
			var fileName = "Peach.license";
			var peachDir = Platform.GetOS() == Platform.OS.Windows ? "Peach" : "peach";
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			var asmDir = Utilities.ExecutionDirectory;

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