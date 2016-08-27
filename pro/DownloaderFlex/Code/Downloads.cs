using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Ionic.Zip;
using Newtonsoft.Json;
using NLog;

namespace PeachDownloader
{
	public class Downloads
	{
		const string ProductStudio = "Peach Studio";

		SortedDownloads _downloads = new SortedDownloads();

		public Downloads()
		{
			var root = ConfigurationManager.AppSettings["Downloads"];
			foreach (var dir in Directory.EnumerateDirectories(root))
			{
				var release = JsonRelease.Load(dir);
				if (release == null)
					continue;

				if (release.Product != ProductStudio)
					continue;

				if (release.Version < 3)
					continue;

				SortedReleases releases;
				if (!_downloads.TryGetValue(release.Product, out releases))
				{
					releases = new SortedReleases();
					_downloads.Add(release.Product, releases);
				}

				releases.Add(Build.Parse(release.Build), release);
			}

			AppSession.Current.Downloads = _downloads;
		}

		public SortedSet<string> GetReleases()
		{
			var builds = new SortedSet<string>();

			foreach (var product in _downloads)
			{
				foreach (var release in product.Value)
				{
					var build = release.Value.Build;
					var prefix = build.Substring(0, build.LastIndexOf(".", StringComparison.Ordinal));
					builds.Add(prefix);
				}
			}

			return builds;
		}

		public IEnumerable<JsonRelease> GetReleases(string prefix)
		{
			foreach (var product in _downloads)
			{
				foreach (var release in product.Value.Where(x => x.Key.ToString().StartsWith(prefix)))
				{
					if (release.Value.Version < 2)
						continue;

					yield return release.Value;
				}
			}
		}

		public static void Download()
		{
			var Request = HttpContext.Current.Request;
			var Response = HttpContext.Current.Response;
			var logger = LogManager.GetCurrentClassLogger();

			if (!AppSession.Current.IsAuthenticated)
			{
				logger.Error("Error 10001: Authenticated == false");
				Response.Write("Error: 10001");
				return;
			}

			if (!AppSession.Current.IsEulaAccepted)
			{
				logger.Error("Error 10002: AcceptLicense == false");
				Response.Write("Error: 10002");
				return;
			}

			var activationId = Request["a"];

			if (string.IsNullOrEmpty(activationId))
			{
				logger.Error("Error 10002.1: ActivationId is null or empty");
				Response.Write("Error: 10002.1");
				return;
			}

			var activations = AppSession.Current.Activations;
			var activation = activations.SingleOrDefault(x => x.ActivationId == activationId);

			AppSession.Current.IsEulaAccepted = false;

			var downloads = AppSession.Current.Downloads;
			if (downloads == null)
			{
				logger.Error("Error 10006: downloads == null");
				Response.Write("Error: 10006");
				return;
			}

			var product = Request["p"];
			var build = Request["b"];
			var filename = Request["f"];

			if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(build) || string.IsNullOrEmpty(filename))
			{
				logger.Error("Error 10002.2: missing one of product, build or file");
				Response.Write("Error: 10002.2");
				return;
			}

			// Validate product
			SortedReleases releases;
			if (!downloads.TryGetValue(product, out releases))
			{
				logger.Error("Error 10003.1: invalid product");
				Response.Write("Error: 10003.1");
				return;
			}

			// Validate build
			JsonRelease release;
			if (!releases.TryGetValue(Build.Parse(build), out release))
			{
				logger.Error("Error 10003.2: invalid build");
				Response.Write("Error: 10003.2");
				return;
			}

			// Validate file
			var file = release.Files.SingleOrDefault(x => x.Name == filename);
			if (file == null)
			{
				logger.Error("Error 10004: invalid file");
				Response.Write("Error: 10004");
				Response.End();
				return;
			}

			// Handle different versions of releases
			if (release.Version < 3)
			{
				logger.Error("Error 10005: Unsupported release version");
				Response.Write("Error: 10005");
				return;
			}

			if (release.Version != 3)
			{
				logger.Error("Error 10008: Unknown version: {0}", release.Version);
				Response.Write("Error: 10008");
				return;
			}

			var tmpFile = Path.Combine(
				ConfigurationManager.AppSettings["TempFolder"],
				Guid.NewGuid().ToString()
			);

			Debug.Assert(File.Exists(file.Path));
			Debug.Assert(!File.Exists(tmpFile));

			File.Copy(file.Path, tmpFile);

			try
			{
				try
				{
					file.BuildDownload(activation, tmpFile);
				}
				catch (Exception ex)
				{
					logger.Error("Error 10006: {0}", ex.ToString());
					Response.Write("Error: 10006");
					return;
				}

				try
				{
					Response.Clear();
					Response.ContentType = "application/octet-stream";
					Response.AppendHeader("Content-Length", new FileInfo(tmpFile).Length.ToString());
					Response.AppendHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", file.Name));
					Response.TransmitFile(tmpFile);
					Response.Flush();
				}
				catch (Exception ex)
				{
					logger.Error("Error 10007: {0}", ex.ToString());
					Response.Write("Error: 10007");
				}
			}
			finally
			{
				// Remove generated file.
				if (File.Exists(tmpFile))
					File.Delete(tmpFile);
			}
		}
	}

	public sealed class BuildComparer : IComparer<Build>
	{
		private readonly IComparer<int> _comparer;

		public BuildComparer()
		{
			_comparer = Comparer<int>.Default;
		}

		public int Compare(Build left, Build right)
		{
			var ret = _comparer.Compare(right.major, left.major);
			if (ret != 0)
				return ret;

			ret = _comparer.Compare(right.minor, left.minor);
			if (ret != 0)
				return ret;

			ret = _comparer.Compare(right.build, left.build);
			return ret;
		}
	}

	/// <summary>
	/// DTO for v3 of release.json
	/// </summary>
	public class JsonRelease
	{
		/// <summary>
		/// Product name "Peach Professional" vs. "Peach Enterprise"
		/// </summary>
		[JsonProperty("product")]
		public string Product { get; set; }

		/// <summary>
		/// Build string "3.4.41"
		/// </summary>
		[JsonProperty("build")]
		public string Build { get; set; }

		/// <summary>
		/// Build date
		/// </summary>
		[JsonProperty("date")]
		public string Date { get; set; }

		/// <summary>
		/// Release files (different architectures)
		/// </summary>
		[JsonProperty("dist")]
		public string[] Dist { get; set; }

		/// <summary>
		/// Version of json in use
		/// </summary>
		[JsonProperty("version")]
		[DefaultValue(1)]
		public int Version { get; set; }

		/// <summary>
		/// Available Pits
		/// </summary>
		[JsonProperty("pit_archives")]
		public JsonPit[] PitArchives { get; set; }

		/// <summary>
		/// Available Pit Packs
		/// </summary>
		[JsonProperty("packs")]
		public PitPacks[] Packs { get; set; }

		public string BasePath { get; set; }

		public static JsonRelease Load(string dir)
		{
			var path = Path.Combine(dir, "release.json");
			if (!File.Exists(path))
				return null;

			using (var stream = File.OpenRead(path))
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				var release = JsonSerializer.CreateDefault().Deserialize<JsonRelease>(json);
				release.BasePath = dir;
				return release;
			}
		}

		public IEnumerable<DownloadFile> Files
		{
			get
			{
				if (Dist == null)
					return new DownloadFile[0];
				return Dist.Select(x => new DownloadFile(this, x));
			}
		}
	}

	public class DownloadFile
	{
		JsonRelease _release;

		const string LicenseConfigTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <appSettings>
        <add key=""licenseUrl"" value=""{0}"" />
        <add key=""activationId"" value=""{1}"" />
    </appSettings>
</configuration>";


		internal DownloadFile(JsonRelease release, string file)
		{
			_release = release;
			Name = file;
			Path = System.IO.Path.Combine(release.BasePath, file);
		}

		public string Name { get; private set; }
		public string Path { get; private set; }

		public long Size
		{
			get { return new FileInfo(Path).Length; }
		}

		public long SizeInMB
		{
			get { return Size / 1048576; }
		}

		public string Url
		{
			get
			{
				var args = HttpUtility.ParseQueryString(string.Empty);
				args.Add("p", _release.Product);
				args.Add("b", _release.Build);
				args.Add("f", Name);
				return string.Format("SelectActivation.cshtml?{0}", args);
			}
		}

		public void BuildDownload(Activation activation, string outFile)
		{
			using (var outZip = new ZipFile(outFile))
			{
				var licenseFile = string.Format(
					LicenseConfigTemplate,
					activation.LicenseServerUrl,
					activation.ActivationId
				);

				outZip.AddEntry("Peach.exe.license.config", Encoding.UTF8.GetBytes(licenseFile));

				outZip.Save(outFile);
			}
		}
	}

	public class PitPacks
	{
		public string name { get; set; }

		/// <summary>
		/// Name of pit listed in PitArchives
		/// </summary>
		public string[] pits { get; set; }
	}

	public class JsonPit
	{
		/// <summary>
		/// name of pit
		/// </summary>
		public string name { get; set; }

		/// <summary>
		/// Zips to include in this pit
		/// </summary>
		public string[] archives { get; set; }
	}

	public class Build
	{
		public int major;
		public int minor;
		public int build;

		public static Build Parse(string str)
		{
			var matches = Regex.Match(str, @"(\d+)\.(\d+)\.(\d+)");
			if (!matches.Success)
				return null;

			var build = new Build
			{
				major = int.Parse(matches.Groups[1].Value),
				minor = int.Parse(matches.Groups[2].Value),
				build = int.Parse(matches.Groups[3].Value)
			};

			return build;
		}

		public override string ToString()
		{
			return string.Format(@"{0}.{1}.{2}", major, minor, build);
		}
	}

	public class SortedReleases : SortedDictionary<Build, JsonRelease>
	{
		public SortedReleases()
			: base(new BuildComparer())
		{
		}
	}

	public class SortedDownloads : SortedDictionary<string, SortedReleases>
	{
	}
}
