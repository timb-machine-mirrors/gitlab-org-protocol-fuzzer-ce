using System;
using System.Configuration;
using System.IO;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public interface ILicenseConfig
	{
		string ActivationId { get; set; }
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

		public string LicenseUrl
		{
			get { return GetConfig(Keys.LicenseUrl); }
			set { SetConfig(Keys.LicenseUrl, value); }
		}

		public string LicensePath
		{
			get
			{
				var path = GetConfig(Keys.LicensePath);
				if (path != null)
					return path;

				path = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
					"Peach"
				);

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				return path;
			}
			set
			{
				SetConfig(Keys.LicensePath, value);
			}
		}

		public byte[] IdentityData
		{
			get { return IdentityClient.IdentityData; }
		}

		public bool DetectConfig
		{
			get { return Utilities.DetectConfig(ConfigFilename); }
		}

		string GetConfig(string name)
		{
			return Utilities.OpenConfig(ConfigFilename).AppSettings.Settings.Get(name);
		}

		void SetConfig(string name, string value)
		{
			var config = Utilities.OpenConfig(ConfigFilename);
			var settings = config.AppSettings.Settings;
			settings.Set(name, value);
			config.Save(ConfigurationSaveMode.Modified);
		}
	}
}