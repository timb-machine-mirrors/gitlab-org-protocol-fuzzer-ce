﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Google;
using Ionic.Zip;
using Newtonsoft.Json;

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

		static void BasicDownload(JsonRelease release, string filename)
		{
			var Request = HttpContext.Current.Request;
			var Response = HttpContext.Current.Response;

			var file = release.FlexnetFiles.SingleOrDefault(x => x.Name == filename);
			if (file == null)
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				Response.Write("Error: 10010");
				return;
			}

			Response.Clear();
			Response.ContentType = "application/octet-stream";
			Response.AppendHeader("Content-Length", file.Size.ToString());
			Response.AppendHeader("Content-Disposition", string.Format("attachment; filename=\"{0}\"", file.Name));
			Response.TransmitFile(file.Path);
			Response.Flush();
		}

		public static void Download()
		{
			var Request = HttpContext.Current.Request;
			var Response = HttpContext.Current.Response;

			if (!AppSession.Current.IsAuthenticated)
			{
				Response.StatusCode = (int)HttpStatusCode.Unauthorized;
				Response.Write("Error: 10001");
				return;
			}

			var product = Request["p"];
			var build = Request["b"];
			var filename = Request["f"];

			if (string.IsNullOrEmpty(product) || string.IsNullOrEmpty(build) || string.IsNullOrEmpty(filename))
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10002");
				return;
			}

			var downloads = AppSession.Current.Downloads;
			if (downloads == null)
			{
				Response.StatusCode = (int)HttpStatusCode.InternalServerError;
				Response.Write("Error: 10003");
				return;
			}

			// Validate product
			SortedReleases releases;
			if (!downloads.TryGetValue(product, out releases))
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10004");
				return;
			}

			// Validate build
			JsonRelease release;
			if (!releases.TryGetValue(Build.Parse(build), out release))
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10005");
				return;
			}

			// Handle different versions of releases
			if (release.Version < 3)
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10006");
				return;
			}

			if (release.Version != 3)
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10007");
				return;
			}

			if (Request["x"] != null)
			{
				BasicDownload(release, filename);
				return;
			}

			if (!AppSession.Current.IsEulaAccepted)
			{
				Response.StatusCode = (int)HttpStatusCode.Forbidden;
				Response.Write("Error: 10008");
				return;
			}

			var activationId = Request["a"];

			if (string.IsNullOrEmpty(activationId))
			{
				Response.StatusCode = (int)HttpStatusCode.BadRequest;
				Response.Write("Error: 10009");
				return;
			}

			var activations = AppSession.Current.Activations;
			var activation = activations.SingleOrDefault(x => x.ActivationId == activationId);

			AppSession.Current.IsEulaAccepted = false;

			// Validate file
			var file = release.Files.SingleOrDefault(x => x.Name == filename);
			if (file == null)
			{
				Response.StatusCode = (int)HttpStatusCode.NotFound;
				Response.Write("Error: 10010");
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
					Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					Response.Write("Error: 10011\r\n");
					Response.Write(ex.ToString());
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
					Response.StatusCode = (int)HttpStatusCode.InternalServerError;
					Response.Write("Error: 10012\r\n");
					Response.Write(ex.ToString());
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

		[JsonProperty("pit_features")]
		public PitFeature[] PitFeatures { get; set; }

		[JsonProperty("flexnetls")]
		public string[] Flexnet { get; set; }

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
				return Dist.Select(x => new DownloadFile(this, x)).OrderBy(x => x.Name);
			}
		}

		public IEnumerable<FlexnetFile> FlexnetFiles
		{
			get
			{
				if (Dist == null)
					return new FlexnetFile[0];
				return Flexnet.Select(x => new FlexnetFile(this, x)).OrderBy(x => x.Name);
			}
		}

	}

	public class DownloadFile
	{
		JsonRelease _release;
		const string PitPrefix = "PeachPit-";

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
			get { return Size / 1024 / 1024; }
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

		string FeatureName(string name)
		{
			return PitPrefix + CityHash.CityHash64(name).ToString("X16");
		}

		public void BuildDownload(Activation activation, string outFile)
		{
			var map = _release.PitFeatures.ToDictionary(x => FeatureName(x.Feature));
			var pendingZips = new Dictionary<string, ZipFile>();

			using (var outZip = new ZipFile(outFile))
			{
				var licenseFile = string.Format(
					LicenseConfigTemplate,
					activation.LicenseServerUrl,
					activation.ActivationId
				);

				outZip.AddEntry("Peach.exe.license.config", licenseFile, Encoding.UTF8);
				outZip.AddFile(System.IO.Path.Combine(_release.BasePath, "pits", "Peach.Pro.Pits.dll"), "pits");

				foreach (var pit in activation.Pits)
				{
					PitFeature feature;
					if (map.TryGetValue(pit, out feature))
					{
						var path = System.IO.Path.Combine(_release.BasePath, feature.Zip);

						ZipFile zip;
						if (!pendingZips.TryGetValue(path, out zip))
						{
							zip = new ZipFile(path);
							pendingZips.Add(path, zip);
						}
						
						foreach (var entry in zip)
						{
							var relpath = System.IO.Path.Combine("pits", entry.FileName);
							if (!feature.Exclude.Contains(entry.FileName) && 
							    !outZip.ContainsEntry(relpath))
							{
								outZip.AddEntry(relpath, x => zip[entry.FileName].OpenReader(), (x, y) => y.Dispose());
							}
						}
					}
				}

				outZip.Save(outFile);
				foreach (var zip in pendingZips.Values)
				{
					zip.Dispose();
				}
			}
		}
	}

	public class FlexnetFile
	{
		JsonRelease _release;

		internal FlexnetFile(JsonRelease release, string file)
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
			get { return Size / 1024 / 1024; }
		}

		public string Url
		{
			get
			{
				var args = HttpUtility.ParseQueryString(string.Empty);
				args.Add("p", _release.Product);
				args.Add("b", _release.Build);
				args.Add("f", Name);
				args.Add("x", "1");
				return string.Format("Download.cshtml?{0}", args);
			}
		}
	}

	public class PitFeature
	{
		[JsonProperty("zip")]
		public string Zip { get; set; }

		[JsonProperty("feature")]
		public string Feature { get; set; }

		[JsonProperty("exclude")]
		public string[] Exclude { get; set; } 
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
