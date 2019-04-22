using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using NLog;
using Org.BouncyCastle.Crypto.Digests;
using Peach.Core;

namespace Peach.Pro.Core.License
{
	public class PeachLicense : ILicense
	{
		static readonly NLog.Logger Logger = LogManager.GetCurrentClassLogger();

		private const string PeachPublicKey = "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEFeCYg3NbYIgtyjqS2SAXaAhNQdShBu7ev0clKqwG5IYceuRnYdaDBmWaRSNNJatR1ld5MzOGIzcCGUXaV0e80g==";

		public static class Keys
		{
			public const string EulaConfig = "eulaAccepted";
			public const string Baseline = "Peach-Baseline";
			public const string NodeLock = "Peach-NodeLock";
			public const string CustomPit = "Peach-CustomPit";
			public const string TestCase = "Peach-TestCase";
			public const string BundlePrefix = "PeachBundle-";
			public const string PitPrefix = "PeachPit-";
		}

		public class ActivationRequest
		{
			public string PublicKey { get; set; }
			public string ActivationId { get; set; }
			public string HostId { get; set; }
			public Dictionary<string, int> Usage { get; set; }
		}

		public enum ResultCode
		{
			Success,
			Failed
		}

		public class ActivationResponse
		{
			public ResultCode Result { get; set; }
			public byte[] License { get; set; }
			public string Error { get; set; }
		}

		private readonly ILicenseConfig _cfg;
		private readonly string _hostId;
		private readonly string _licenseFile;
		private readonly Uri _licenseUrl;
		private readonly Uri _proxy;

		Portable.Licensing.License _license;

		public PeachLicense(ILicenseConfig cfg, LicenseOptions options)
		{
			Status = LicenseStatus.Missing;
			ErrorText = string.Empty;

			_cfg = cfg;
			_licenseUrl = new Uri(_cfg.LicenseUrl);
			_licenseFile = Path.Combine(_cfg.LicensePath, _cfg.ActivationId);
			_hostId = GetHostId();
			_proxy = GetProxy(_licenseUrl);

			Initialize(options);
		}

		public void Dispose()
		{
		}

		private void Initialize(LicenseOptions options)
		{
			if (_cfg.ActivationId == null)
				return;

			TryLoadLicense();

			RefreshLicenseStatus();

			if (options.Deactivate)
				Deactivate();
			else if (Status != LicenseStatus.Valid || options.ForceActivation)
				Activate();

			//Dump();
		}

		void RefreshLicenseStatus()
		{
			Status = LicenseStatus.Invalid;
			Expiration = DateTime.MaxValue;

			if (_license == null)
			{
				Status = LicenseStatus.Missing;
				Logger.Debug("RefreshLicenseStatus> License is missing");
				return;
			}

			if (!_license.VerifySignature(PeachPublicKey))
			{
				Logger.Debug("RefreshLicenseStatus> License signature is invalid");
				return;
			}

			if (_cfg.ActivationId != _license.AdditionalAttributes.Get("ActivationID"))
			{
				Logger.Debug("RefreshLicenseStatus> Activation ID mismatch");
				return;
			}

			if (_hostId != _license.AdditionalAttributes.Get("HostID"))
			{
				Logger.Debug("RefreshLicenseStatus> Activation ID mismatch");
				return;
			}

			// Display the expiration as the StopDate of the license
			var stopDate = _license.AdditionalAttributes.Get("StopDate");
			Expiration = DateTime.ParseExact(stopDate, "r", CultureInfo.InvariantCulture);

			// Use the license expiration (borrow interval) for validity checking
			if (_license.Expiration < DateTime.Now)
			{
				Status = LicenseStatus.Expired;
				return;
			}

			if (!HasFeature(Keys.Baseline))
			{
				Status = LicenseStatus.Missing;
				return;
			}

			Status = LicenseStatus.Valid;
		}

		public LicenseStatus Status { get; private set; }
		public string ErrorText { get; private set; }
		public DateTime Expiration { get; private set; }

		public bool EulaAccepted
		{
			get
			{
				var config = Utilities.GetUserConfig();
				var str = config.AppSettings.Settings.Get(Keys.EulaConfig) ?? string.Empty;
				bool value;
				var parsed = bool.TryParse(str, out value);
				return parsed && value;
			}
			set
			{
				var config = Utilities.GetUserConfig();
				var settings = config.AppSettings.Settings;
				settings.Set(Keys.EulaConfig, value.ToString());
				config.Save(ConfigurationSaveMode.Modified);
			}
		}

		public string EulaText
		{
			get
			{
				var res = "Peach.Pro.Core.Resources.Eula.{0}.txt".Fmt(Eula);
				return Utilities.LoadStringResource(Assembly.GetExecutingAssembly(), res);
			}
		}

		public EulaType Eula
		{
			get { return EulaType.Flex; }
		}

		public PitFeature CanUsePit(string path)
		{
			var pitName = string.Join("/",
				Path.GetFileName(Path.GetDirectoryName(path)),
				Path.GetFileName(path)
			);

			var pit = _cfg.Manifest.Features.FirstOrDefault(x => x.Value.Pit == pitName);
			if (pit.Key == null)
			{
				return new PitFeature
				{
					Name = Keys.CustomPit,
					Path = path,
					IsValid = HasFeature(Keys.CustomPit),
					IsCustom = true,
				};
			}

			return new PitFeature
			{
				Name = pit.Key,
				Path = path,
				IsValid = HasFeature(Keys.PitPrefix + pit.Value.Legacy),
				Key = MakeKey(pit.Key),
			};
		}

		public bool CanUseMonitor(string name, string category)
		{
			return HasFeature(Keys.BundlePrefix + category);
		}

		public IJobLicense NewJob(string pit, string config, string job)
		{
			return new JobLicense(this);
		}

		protected bool IsMetered(string featureName)
		{
			return _license.ProductFeatures.GetFeature(featureName).Get("usage") != null;
		}

		protected bool HasFeature(string featureName)
		{
			return _license.ProductFeatures.Contains(featureName);
		}

		protected bool CanExecuteTestCase()
		{
			var feature = _license.ProductFeatures.GetFeature(Keys.TestCase);
			if (feature == null)
				return false;

			var countStr = feature.Get("count");

			// Uncounted
			if (string.IsNullOrEmpty(countStr))
				return true;

			var usageStr = feature.Get("usage");

			int count;
			int usage;

			if (!int.TryParse(countStr, out count) || !int.TryParse(usageStr, out usage))
				return false;

			return usage < count;
		}

		private static string GetHostId()
		{
			var nics = NetworkInterface.GetAllNetworkInterfaces();

			foreach (var nic in nics)
			{
				Console.WriteLine("{0} -> {1}", nic.Name, nic.GetPhysicalAddress().ToString());
			}
			return "TODO";
		}

		private static Uri GetProxy(Uri uri)
		{
			if (Platform.GetOS() == Platform.OS.Linux)
			{
				var env = uri.Scheme + "_proxy";
				var proxyStr = Environment.GetEnvironmentVariable(env);

				Logger.Debug("GetProxy({0}) {1}={2}", uri, env, proxyStr);

				Uri proxy;
				if (proxyStr != null && Uri.TryCreate(proxyStr, UriKind.Absolute, out proxy))
					return proxy;
			}

			return null;
		}

		private HttpClient GetHttpClient()
		{
			// TODO: Figure out how to set this on the HttpClientHandler
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

			if (_proxy == null)
				return new HttpClient();

			Logger.Debug("Using Proxy: {0}:{1}", _proxy.Host, _proxy.Port);

			var handler = new HttpClientHandler
			{
				Proxy = new WebProxy(_proxy.Host, _proxy.Port),
				UseProxy = true
			};

			return new HttpClient(handler);
		}

		private void TryLoadLicense()
		{
			try
			{
				using (var s = File.OpenRead(_licenseFile))
				{
					_license = Portable.Licensing.License.Load(s);
				}

				Logger.Trace("Loaded license from {0}", _licenseFile);
			}
			catch (Exception ex)
			{
				Logger.Trace(ex, "Failed to load license from {0}", _licenseFile);
			}
		}

		private void Deactivate()
		{
			throw new NotImplementedException();
		}

		private void Activate()
		{
			Console.WriteLine("Activating license, this may take a few moments.");
			Console.WriteLine("Activation ID: {0}", _cfg.ActivationId);
			Console.WriteLine("Host ID: {0}", _hostId);
			Console.WriteLine("License Server URL: {0}", _licenseUrl);

			if (_proxy != null)
				Console.WriteLine("Using proxy: {0}:{1}", _proxy.Host, _proxy.Port);

			Sync(0, false);

			RefreshLicenseStatus();

			switch (Status)
			{
				case LicenseStatus.Valid:
					Console.WriteLine("License has been activated.");
					break;
				case LicenseStatus.Invalid:
					Console.WriteLine("License is invalid.");
					break;
				case LicenseStatus.Expired:
					Console.WriteLine("License has expired.");
					break;
				case LicenseStatus.Missing:
					Console.WriteLine("License is missing.");
					break;
			}
		}


		private static byte[] MakeKey(string feature)
		{
			var saltBytes = System.Text.Encoding.UTF8.GetBytes("^PeachFuzzer$");
			var password = System.Text.Encoding.UTF8.GetBytes(feature);

			var digest = new Sha256Digest();
			var key = new byte[digest.GetDigestSize()];

			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.BlockUpdate(password, 0, password.Length);
			digest.BlockUpdate(saltBytes, 0, saltBytes.Length);
			digest.DoFinal(key, 0);

			return key;
		}

		protected void Sync(int count, bool retry)
		{
			try
			{
				using (var client = GetHttpClient())
				{
					var request = new ActivationRequest
					{
						ActivationId = _cfg.ActivationId,
						HostId = _hostId,
						PublicKey = PeachPublicKey,
					};

					if (count > 0)
					{
						request.Usage = new Dictionary<string, int>
						{
							{Keys.TestCase, count}
						};
					}

					var uri = new Uri(_licenseUrl, "activate");
					var httpResponse = client.PostAsJsonAsync(uri, request).Result;

					if (!httpResponse.IsSuccessStatusCode)
					{
						throw new ApplicationException(
							"The specified licenseUrl ({0}) is invalid.\n".Fmt(_cfg.LicenseUrl) +
							"Update the 'licenseUrl' setting in of your Peach.license.config file and try again."
						);
					}

					var response = httpResponse.Content.ReadAsAsync<ActivationResponse>().Result;

					if (response.Result != ResultCode.Success)
						throw new NotSupportedException();

					_license = Portable.Licensing.License.Load(new MemoryStream(response.License));

					File.WriteAllBytes(_licenseFile, response.License);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				throw;
			}
		}

		class JobLicense : IJobLicense
		{
			private readonly PeachLicense _parent;
			private readonly bool _isMetered;

			private bool _isFirst = true;
			private int _counter;

			public JobLicense(PeachLicense parent)
			{
				_parent = parent;
				_isMetered = _parent.IsMetered(Keys.TestCase);
			}

			public bool CanExecuteTestCase()
			{
				// Node locked is not metered so don't report usage
				if (!_isMetered)
					return true;

				if (_isFirst || _counter >= 100)
				{
					_isFirst = false;

					_parent.Sync(_counter, false);

					_counter = 0;

					if (!_parent.CanExecuteTestCase())
						return false;
				}

				_counter++;
				return true;
			}

			public void Dispose()
			{
				if (_counter > 0)
				{
					_parent.Sync(_counter, false);

					_counter = 0;
				}
			}
		}
	}
}
