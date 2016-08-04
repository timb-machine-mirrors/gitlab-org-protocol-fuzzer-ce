using System;
using System.IO;
using System.Collections.Generic;
using System.Web.UI;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace PeachDownloader
{
	public partial class Downloads : Page
	{
		private const string ProductPro = "Peach Professional";
		private const string ProductEnt = "Peach Enterprise";
		private const string ProductStudio = "Peach Studio";

		protected SortedDictionary<string, SortedDictionary<Build, JsonRelease>> _downloads = new SortedDictionary<string, SortedDictionary<Build, JsonRelease>>();

		protected void Page_Load(object sender, EventArgs e)
		{
		    var downloads = new SortedDictionary<string, SortedDictionary<string, JsonRelease>>();
			foreach (var buildDir in Directory.EnumerateDirectories((string)Application["Downloads"]))
			{
				try
				{
					var releaseFile = Path.Combine(buildDir, "release.json");
					if (!File.Exists(releaseFile))
						continue;

					var release = JsonConvert.DeserializeObject<JsonRelease>(File.ReadAllText(releaseFile));
					release.basePath = buildDir;

					// Note: Starting with v3.6, we will not have different bits between enterprise and pro
					//       the release.product will be "Peach Studio" and should show up for all license
					//       types.

					// Only show enterprise products to enterprise customers
					if (release.product == ProductEnt && !(bool)Session["LicenseEnt"])
						continue;

					// Only show professional products to pro customers
					if (release.product == ProductPro && !(bool)Session["LicensePro"])
						continue;

					if (!_downloads.ContainsKey(release.product))
					{
						_downloads[release.product] = new SortedDictionary<Build, JsonRelease>(new BuildComparer());
						downloads[release.product] = new SortedDictionary<string, JsonRelease>();
					}

					_downloads[release.product][Build.Parse(release.build)] = release;
					downloads[release.product][release.build] = release;
				}
				catch
				{
					System.Diagnostics.Debugger.Break();
				}
			}

			Session["Downloads"] = downloads;
		}


        protected SortedSet<string> GetReleases()
	    {

            var builds = new SortedSet<string>();

	        foreach (var product in _downloads.Keys)
	        {
	            foreach (var build in _downloads[product].Keys)
	            {
	                var release = _downloads[product][build];

                    var version = release.build.Substring(0, release.build.LastIndexOf(".", StringComparison.Ordinal));
                    if(!builds.Contains(version))
                        builds.Add(version);
	            }
	        }

	        return builds;
	    }

		protected int FileSize(string basePath, string file)
		{
			var info = new FileInfo(Path.Combine(basePath, file));
			return (int) info.Length/1048576;
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
	/// Deserialize v1 release.json into this class
	/// </summary>
	/// <remarks>
	/// This is the origional release json format. In this version
	/// pits are distributed as a single zip containing all pits.
	/// </remarks>
	public class JsonRelease
	{
		/// <summary>
		/// Product name "Peach Professional" vs. "Peach Enterprise"
		/// </summary>
		public string product { get; set; }

		/// <summary>
		/// Build string "3.4.41"
		/// </summary>
		public string build { get; set; }

		/// <summary>
		/// Is this a nightly or stable build?
		/// </summary>
		public bool nightly { get; set; }

		/// <summary>
		/// Build date
		/// </summary>
		public string date { get; set; }

		/// <summary>
		/// Filename of pits archive
		/// </summary>
		public string pits { get; set; }

		/// <summary>
		/// Release files (different architectures)
		/// </summary>
		public string[] files { get; set; }

		/// <summary>
		/// Base build path on disk
		/// </summary>
		public string basePath { get; set; }

		/// <summary>
		/// Filename of trial pits archive
		/// </summary>
		public string trial { get; set; }

		//////////////////////////////////////////////////////
		// Version 2 fields

		/// <summary>
		/// Version of json in use
		/// </summary>
		[JsonProperty("version")]
		[System.ComponentModel.DefaultValue(1)]
		public int Version { get; set; }

		/// <summary>
		/// Pit's to include in trial.
		/// </summary>
		[JsonProperty("trial_archives")]
		public JsonPit[] TrialArchives { get; set; }

		/// <summary>
		/// Available Pits
		/// </summary>
		[JsonProperty("pit_archives")]
		public JsonPit[] PitArchives { get; set; }

		/// <summary>
		/// Available Pit Packs
		/// </summary>
		[JsonProperty("packs")]
		public JsonPacks[] Packs { get; set; }
	}

	public class JsonPacks
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

    public class Release
    {
        public bool IsStable = false;
        public string Version = null;
    }

	public class Build
	{
		public int major = 0;
		public int minor = 0;
		public int build = 0;

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
}
