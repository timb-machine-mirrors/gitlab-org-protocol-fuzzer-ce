using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Peach.Core;
using Portable.Licensing.Validation;

namespace Peach.Pro.Core
{
	public enum LicenseFeature
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

	public interface ILicense
	{
		string ErrorText { get; }
		LicenseFeature Version { get; }
		DateTime Expiration { get; }
		bool IsMissing { get; }
		bool IsExpired { get; }
		bool IsInvalid { get; }
		bool IsNearingExpiration { get; }
		int ExpirationInDays { get; }
		bool IsValid { get; }
		bool EulaAccepted { get; set; }
		string EulaText();
		string EulaText(LicenseFeature version);
	}

	public class License : ILicense
	{
		public const string ExpirationWarning = "Warning: Peach expires in {0} days";
		readonly string EulaConfig = "eulaAccepted";
		bool? eulaAccepted;
		bool valid;
		readonly object mutex = new object();

		public License()
		{
			Verify();
		}

		public static ILicense Instance
		{
			get { return new License(); }
		}

		public string ErrorText { get; private set; }
		public LicenseFeature Version { get; private set; }
		public DateTime Expiration { get; private set; }
		public bool IsMissing { get; private set; }
		public bool IsExpired { get; private set; }
		public bool IsInvalid { get; private set; }

		public bool IsNearingExpiration
		{
			get { return IsValid && Expiration < DateTime.Now.AddDays(30); }
		}

		public int ExpirationInDays
		{
			get
			{
				Debug.Assert(IsValid);
				return (Expiration - DateTime.Now).Days;
			}
		}

		public bool IsValid
		{
			get
			{
				lock (mutex)
				{
					if (valid)
						return true;

					valid = Verify();
					return valid;
				}
			}
		}

		public bool EulaAccepted
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

						var parsed = bool.TryParse(str, out val);
						eulaAccepted = parsed & val;
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

		public string EulaText()
		{
			return EulaText(Version);
		}

		public string EulaText(LicenseFeature version)
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

		LicFile Read(string path)
		{
			var ret = new LicFile { FileName = path };

			try
			{
				if (!File.Exists(path))
				{
					ret.Exception = new FileNotFoundException("Could not find file '" + path + "'.", path);
				}
				else
				{
					ret.Contents = File.ReadAllText(path);
				}
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

		LicFile Read(out string licenseFile)
		{
			// Try several different filenames
			// this reduces the number of support calls.

			var searchPaths = new List<string>
			{
				Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
			};

			var peachDirs = new[]
			{
				"peach",
				"Peach",
			};

			var fileNames = new[]
			{
				"Peach.license",
				"Peach.license.xml",
				"Peach.license.txt",

				"peach.license",
				"peach.license.xml",
				"peach.license.txt"
			};

			if (Platform.GetOS() == Platform.OS.OSX)
			{
				// This is to allow users to install Peach.license on El Capitan.
				// El Capitan introduced SIP which prevents users from creating files
				// in "system" directories, such as "/usr/share".
				searchPaths.Add("/Library/Application Support");
			}

			var asmDir = Utilities.ExecutionDirectory;
			Exception ex = null;

			foreach (var fileName in fileNames)
			{
				// Try location of peach.exe first
				var local = Path.Combine(asmDir, fileName);
				var localLic = Read(local);

				if (localLic.Exception == null)
				{
					licenseFile = local;
					return localLic;
				}

				// Try common application data second
				// Windows   C:\ProgramData\Peach\Peach.license
				// Linux     /usr/share/peach/Peach.license
				// OSX       /usr/share/peach/Peach.license
				//           /Library/Application Support/Peach/Peach.license
				foreach (var rootDir in searchPaths)
				{
					foreach (var peachDir in peachDirs)
					{
						var global = Path.Combine(rootDir, peachDir, fileName);
						var globalLic = Read(global);

						if (globalLic.Exception == null)
						{
							licenseFile = global;
							return globalLic;
						}
					}
				}

				// Rethrow local exception since that is technically the first error
				var inner = localLic.Exception;

				// Skip these
				if (inner is FileNotFoundException || inner is DirectoryNotFoundException)
					continue;

				ex = (Exception)Activator.CreateInstance(inner.GetType(), inner.Message, inner);
			}

			if (ex == null)
				throw new FileNotFoundException("Unable to locate license file 'Peach.license'.");

			throw ex;
		}

		bool Verify()
		{
			Version = LicenseFeature.Unknown;
			Expiration = DateTime.MinValue;
			IsInvalid = false;
			IsExpired = false;
			IsMissing = true;
			ErrorText = string.Empty;

			string licenseFile = null;

			try
			{
				const string publicKey = "MIIBKjCB4wYHKoZIzj0CATCB1wIBATAsBgcqhkjOPQEBAiEA/////wAAAAEAAAAAAAAAAAAAAAD///////////////8wWwQg/////wAAAAEAAAAAAAAAAAAAAAD///////////////wEIFrGNdiqOpPns+u9VXaYhrxlHQawzFOw9jvOPD4n0mBLAxUAxJ02CIbnBJNqZnjhE50mt4GffpAEIQNrF9Hy4SxCR/i85uVjpEDydwN9gS3rM6D0oTlF2JjClgIhAP////8AAAAA//////////+85vqtpxeehPO5ysL8YyVRAgEBA0IABBXgmINzW2CILco6ktkgF2gITUHUoQbu3r9HJSqsBuSGHHrkZ2HWgwZlmkUjTSWrUdZXeTMzhiM3AhlF2ldHvNI=";
				var xml = Read(out licenseFile);

				IsMissing = false;

				var license = Portable.Licensing.License.Load(xml.Contents);

				var failures = license.Validate()
					.ExpirationDate()
					//.When(lic => lic.Type == LicenseType.Trial)
					.And()
					//.AssertThat(lic => lic.ProductFeatures.Contains("Professional"))
					.Signature(publicKey)
					.AssertValidLicense()
					.ToList();

				Expiration = license.Expiration;

				if (failures.OfType<LicenseExpiredValidationFailure>().Any())
				{
					IsExpired = true;
				}
				else if (!failures.Any())
				{
					var ver = Enum.GetNames(typeof(LicenseFeature)).FirstOrDefault(n => license.ProductFeatures.Contains(n));
					if (string.IsNullOrEmpty(ver))
						ver = "Unknown";

					Version = (LicenseFeature)Enum.Parse(typeof(LicenseFeature), ver);

					if (Version != LicenseFeature.Unknown)
						return true;

					IsInvalid = true;
				}
				else
				{
					IsInvalid = true;
				}
			}
			catch (Exception ex)
			{
				if (!IsMissing)
				{
					IsInvalid = true;
					Debug.Assert(licenseFile != null);
					var errorLog = Utilities.GetAppResourcePath("Peach.error.txt");

					try
					{
						File.WriteAllText(errorLog, ex.ToString());
					}
					catch
					{
						// ignored
					}

					ErrorText = "The currently installed license is invalid.\nPlease contact Peach Support at supper@peachfuzzer.com to resolve this issue.\nPlease attach a copy of the current license \"{0}\" and the Peach error log \"{1}\" with your support request.".Fmt(licenseFile, errorLog);
					return false;
				}
			}

			if (IsMissing)
			{
				Debug.Assert(!IsInvalid);
				licenseFile = Utilities.GetAppResourcePath("Peach.license");
				ErrorText = "Peach was unable to locate your license file.\nPlease install your license to \"{0}\" and try again.\nIf the problem persists please contact Peach Support at support@peachfuzzer.com for help in resolving this issue.".Fmt(licenseFile);
			}
			else if (IsExpired)
			{
				Debug.Assert(!IsInvalid);
				Debug.Assert(licenseFile != null);
				ErrorText = "Your license has expired.\nPlease contact the Peach Sales team at sales@peachfuzzer.com to renew your license.";
			}
			else
			{
				Debug.Assert(IsInvalid);
				Debug.Assert(licenseFile != null);
				ErrorText = "The currently installed license is invalid.\nPlease contact Peach Support at supper@peachfuzzer.com to resolve this issue.\nPlease attach a copy of the current license \"{0}\" with your support request.".Fmt(licenseFile);
			}

			return false;
		}
	}
}
