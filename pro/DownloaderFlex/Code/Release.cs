using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Google;
using Ionic.Zip;
using Newtonsoft.Json;

namespace PeachDownloader
{
	/// <summary>
	/// DTO for v3 of release.json
	/// </summary>
	public class Release
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

		public static Release Load(string dir)
		{
			var path = Path.Combine(dir, "release.json");
			if (!File.Exists(path))
				return null;

			using (var stream = File.OpenRead(path))
			using (var reader = new StreamReader(stream))
			using (var json = new JsonTextReader(reader))
			{
				var release = JsonSerializer.CreateDefault().Deserialize<Release>(json);
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
		Release _release;

		const string LicenseConfigTemplate = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
    <appSettings>
        <add key=""licenseUrl"" value=""{0}"" />
        <add key=""activationId"" value=""{1}"" />
    </appSettings>
</configuration>";


		internal DownloadFile(Release release, string file)
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
			return Operations.PitPrefix + CityHash.CityHash64(name).ToString("X16");
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
		Release _release;

		internal FlexnetFile(Release release, string file)
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

	public sealed class BuildComparer : IComparer<Build>
	{
		readonly IComparer<int> _comparer;

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

	public class SortedReleases : SortedDictionary<Build, Release>
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

