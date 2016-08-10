using System;
using System.Configuration;
using System.IO;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public interface ILicenseConfig
	{
		string ActivationId { get; set; }
		byte[] IdentityData { get; }
		string LicenseUrl { get; set; }
		string LicensePath { get; set; }
		PitManifest Manifest { get; set; }
	}

	class LicenseConfig : ILicenseConfig
	{
		const string ConfigFilename = "Peach.license.config";

		static class Keys
		{
			public const string LicenseUrl = "licenseUrl";
			public const string LicensePath = "licensePath";
			public const string ActivationId = "activationId";
		}

		public string ActivationId
		{
			get { return GetConfig(Keys.ActivationId); }
			set { SetConfig(Keys.ActivationId, value); }
		}

		public PitManifest Manifest { get; set; }

		public string LicensePath
		{
			get
			{
				var path = GetConfig(Keys.LicensePath);
				if (path != null)
					return path;

				if (Platform.GetOS() == Platform.OS.OSX)
				{
					// El Capitan introduced SIP which prevents users from creating files
					// in "system" directories, such as "/usr/share".
					return "/Library/Application Support/Peach";
				}

				return Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					"Peach"
					);
			}

			set { SetConfig(Keys.LicensePath, value); }
		}

		public string LicenseUrl
		{
			get { return GetConfig(Keys.LicenseUrl); }
			set { SetConfig(Keys.LicenseUrl, value); }
		}

		public byte[] IdentityData
		{
			get { return IdentityClient_Production.IdentityData; }
		}

		string GetConfig(string name)
		{
			return Utilities.GetUserConfig(ConfigFilename).AppSettings.Settings.Get(name);
		}

		void SetConfig(string name, string value)
		{
			var config = Utilities.GetUserConfig(ConfigFilename);
			var settings = config.AppSettings.Settings;
			settings.Set(name, value);
			config.Save(ConfigurationSaveMode.Modified);
		}
	}
}