using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Portable.Licensing.Validation;

namespace Peach.Core
{
	public static class License
	{
		static readonly string EulaConfig = "eulaAccepted";
		static readonly StringBuilder eulaErrors = new StringBuilder();
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
			Academic,
			TestSuites,
			Studio,
			Developer,
			Unknown,
		}

		static License()
		{
			Verify();
		}

		public static string ErrorText { get { return eulaErrors.ToString(); } }
		public static bool IsValid { get; private set; }
		public static Feature Version { get; private set; }
		public static DateTime Expiration { get; private set; }

		public static bool EulaAccepted
		{
			get
			{
				lock (mutex)
				{
					if (!eulaAccepted.HasValue)
					{
						var config = Utilities.GetUserConfig();
						var str = config.AppSettings.Settings.Get(EulaConfig) ?? string.Empty;
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

					var config = Utilities.GetUserConfig();
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

		class LicFile
		{
			public string FileName { get; set; }
			public string Contents { get; set; }
			public Exception Exception { get; set; }
		}

		static LicFile Read(string path)
		{
			var ret = new LicFile { FileName = path };

			try
			{
				ret.Contents = File.ReadAllText(path);
			}
			catch (FileNotFoundException ex)
			{
				ret.Exception = ex;
			}
			catch (DirectoryNotFoundException ex)
			{
				ret.Exception = ex;
			}

			return ret;
		}

		static LicFile Read()
		{
			// Try several different filenames
			// this reduces the number of support calls.
			var fileNames = new []
			{
				"Peach.license",
				"Peach.license.xml",
				"Peach.license.txt",

				"peach.license",
				"peach.license.xml",
				"peach.license.txt"
			};

			var peachDir = Platform.GetOS() == Platform.OS.Windows ? "Peach" : "peach";
			var appData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			var asmDir = Utilities.ExecutionDirectory;
			Exception ex = null;

			foreach (var fileName in fileNames)
			{
				// Try location of peach.exe first
				var local = Path.Combine(asmDir, fileName);
				var localLic = Read(local);

				if (localLic.Exception == null)
					return localLic;

				// Try common application data second
				// Windows - C:\ProgramData\Peach\Peach.license
				// Unix - /usr/share/peach/Peach.license
				var global = Path.Combine(appData, peachDir, fileName);
				var globalLic = Read(global);

				if (globalLic.Exception == null)
					return globalLic;

				// Rethrow local exception since that is technically the first error
				var inner = localLic.Exception;

				// Skip these
				if (inner is FileNotFoundException || inner is DirectoryNotFoundException)
					continue;

				ex = (Exception) Activator.CreateInstance(inner.GetType(), inner.Message, inner);
			}

			if(ex == null)
				throw new FileNotFoundException("Error, unable to locate license file 'Peach.license'.");

			throw ex;
		}

		static void Verify()
		{
			try
			{
				const string publicKey = "MIIBKjCB4wYHKoZIzj0CATCB1wIBATAsBgcqhkjOPQEBAiEA/////wAAAAEAAAAAAAAAAAAAAAD///////////////8wWwQg/////wAAAAEAAAAAAAAAAAAAAAD///////////////wEIFrGNdiqOpPns+u9VXaYhrxlHQawzFOw9jvOPD4n0mBLAxUAxJ02CIbnBJNqZnjhE50mt4GffpAEIQNrF9Hy4SxCR/i85uVjpEDydwN9gS3rM6D0oTlF2JjClgIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBA0IABBXgmINzW2CILco6ktkgF2gITUHUoQbu3r9HJSqsBuSGHHrkZ2HWgwZlmkUjTSWrUdZXeTMzhiM3AhlF2ldHvNI=";
				var xml = Read();
				var license = Portable.Licensing.License.Load(xml.Contents);

				var failures = license.Validate()
					.ExpirationDate()
					//.When(lic => lic.Type == LicenseType.Trial)
					.And()
					//.AssertThat(lic => lic.ProductFeatures.Contains("Professional"))
					.Signature(publicKey)
					.AssertValidLicense()
					.ToList();

				IsValid = failures.Count == 0;
				Expiration = license.Expiration;

				if (!IsValid)
				{
					eulaErrors.AppendFormat("License file '{0}' failed to verify.", xml.FileName);
					eulaErrors.AppendLine();

					foreach (var failure in failures)
					{
						eulaErrors.AppendFormat("{0}  {1}", failure.Message, failure.HowToResolve);
						eulaErrors.AppendLine();
					}
				}

				var ver = Enum.GetNames(typeof(Feature)).FirstOrDefault(n => license.ProductFeatures.Contains(n));
				if (string.IsNullOrEmpty(ver))
					ver = "Unknown";

				Version = (Feature)Enum.Parse(typeof(Feature), ver);

				if (Version == Feature.Unknown)
					throw new NotSupportedException("No supported features were found in '{0}'.".Fmt(xml.FileName));
			}
			catch (Exception ex)
			{
				IsValid = false;
				Version = Feature.Unknown;

				eulaErrors.AppendLine("License verification failed.");
				eulaErrors.AppendLine(ex.Message);
			}
		}
	}
}
