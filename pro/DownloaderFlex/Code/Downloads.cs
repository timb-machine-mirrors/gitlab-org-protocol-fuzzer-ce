using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

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
				var release = Release.Load(dir);
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

		public IEnumerable<Release> GetReleases(string prefix)
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

		static void BasicDownload(Release release, string filename)
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
			Release release;
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
}
