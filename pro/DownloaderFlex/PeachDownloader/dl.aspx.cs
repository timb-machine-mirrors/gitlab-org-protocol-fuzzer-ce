using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Web.Configuration;
using System.Collections.Generic;
using System.Text;
using Ionic.Zip;
using NLog;

namespace PeachDownloader
{
	/// <summary>
	/// Send a binary file back to the user
	/// </summary>
	public partial class dl : System.Web.UI.Page
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		const string PitPrefix = "pit:";
		const string PackPrefix = "pack:";

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!(bool)Session["LicenseValidation"])
			{
				logger.Error("Error 10001: LicenseValidation == false");

                Response.Write("Error: 10001");
				return;
			}

			if (!(bool)Session["AcceptLicense"])
			{
				logger.Error("Error 10002: AcceptLicense == false");

				Response.Write("Error: 10002");
				return;
			}

			Session["AcceptLicense"] = false;

			var downloads = Session["Downloads"] as SortedDictionary<string, SortedDictionary<string, JsonRelease>>;
			if(downloads == null)
			{
				logger.Error("Error 10006: downloads == null");

				Response.Write("Error: 10006");
				return;
			}

			var product = Request["p"];
			var build = Request["b"];
			var file = Request["f"];

			// Validate product, build
			if (!downloads.ContainsKey(product) || !downloads[product].ContainsKey(build))
			{
				logger.Error("Error 10003: !downloads.ContainsKey(product) || !downloads[product].ContainsKey(build)");

				Response.Write("Error: 10003");
				return;
			}

			var rel = downloads[product][build];

			// Validate file
			if (!ValidateFile(rel, file))
			{
				logger.Error("Error 10004: !ValidateFile(rel, file)");

				Response.Write("Error: 10004");
				Response.End();
				return;
			}

			// Send request file
			Response.Clear();

			Response.AppendHeader("Content-Disposition", "attachment; filename=\"" + file + "\"");
			Response.AddHeader("Extension", file.Substring(
				file.LastIndexOf('.') + 1).ToLower()); 
			
			Response.ContentType = "application/octet-stream";

			// Handle different versions of releases

			if(rel.Version < 1)
			{
				try
				{
					Response.WriteFile(Path.Combine(rel.basePath, file));
				}
				catch (Exception ex)
				{
					logger.Error("Error 10005: {0}", ex.ToString());

					Response.Write("Error: 10005");
					return;
				}

				return;
			}

			if (rel.Version == 2)
			{
				var dlFile = string.Empty;

				try
				{
					// Build download
					dlFile = BuildDownload(rel, Path.Combine(rel.basePath, file));
				}
				catch(Exception ex)
				{
					logger.Error("Error 10006: {0}", ex.ToString());

					Response.Write("Error: 1006");
					return;
				}

				try
				{
					if (!File.Exists(dlFile))
						logger.Error("dlFile doesn not exist '{0}'", dlFile);

					using (var sout = new FileStream(dlFile, FileMode.Open, FileAccess.Read))
					{
						var buff = new byte[1024*10];
						int readLength;

						do
						{
							readLength = sout.Read(buff, 0, buff.Length);

							if (readLength < buff.Length)
							{
								var mout = new MemoryStream(buff, 0, readLength);
								Response.BinaryWrite(mout.ToArray());
							}
							else
								Response.BinaryWrite(buff);

						} while (readLength > 0);
					}

					return;
				}
				catch (Exception ex)
				{
					logger.Error("Error 10007: {0}", ex.ToString());

					Response.Write("Error: 10007");
					return;
				}
				finally
				{
					// Remove generated file.

					if(File.Exists(dlFile))
						File.Delete(dlFile);
				}
			}

			logger.Error("Error 10008: Unkown version: {0}", rel.Version);

			Response.Write("Error: 1008");
		}

		/// <summary>
		/// Validate that file is contained in our release object.
		/// </summary>
		/// <param name="release"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		bool ValidateFile(JsonRelease release, string file)
		{
			var linfo = Session["License"] as LicenseInfo;

			if (release.pits == file)
			{
				return linfo.PitsDownload;
			}

			if (release.trial == file)
			{
				return linfo.Trial;
			}

			return release.files.Contains(file);
		}

		/// <summary>
		/// Create a customer download containing only licensed pits.
		/// </summary>
		/// <param name="release"></param>
		/// <param name="file"></param>
		/// <returns></returns>
		string BuildDownload(JsonRelease release, string file)
		{
			var linfo = Session["License"] as LicenseInfo;
			var outFile = Path.Combine(WebConfigurationManager.AppSettings["TempFolder"], 
				Guid.NewGuid().ToString());

			if (linfo == null)
			{
				logger.Error("Session['License'] returned null.");
				throw new ApplicationException("Session['License'] returned null.");
			}

			Debug.Assert(File.Exists(file));
			Debug.Assert(!File.Exists(outFile));

			File.Copy(file, outFile);

			try
			{
				using (var outZip = new ZipFile(outFile))
				{
					foreach(var pit in linfo.Pits)
					{
						var pitArchive = release.PitArchives.FirstOrDefault(p => p.name == pit);
						if (pitArchive == null)
						{
							logger.Error("pitArchive is null. pit: {0}", pit);
							throw new ApplicationException(string.Format("pitArchive is null. pit: {0}", pit));
						}

						foreach (var filename in pitArchive.archives)
						{
							var archive = Path.Combine(release.basePath, 
								filename.Replace('/', Path.DirectorySeparatorChar));

							Debug.Assert(File.Exists(archive));

							outZip.Merge("pits", archive);
						}
					}

					foreach(var pack in linfo.Packs)
					{
						var packArchive = release.Packs.FirstOrDefault(p => p.name == pack);
						if (packArchive == null)
						{
							logger.Error("packArchive is null. pack: {0}", pack);
							throw new ApplicationException(string.Format("packArchive is null. pack: {0}", pack));
						}

						foreach(var pit in packArchive.pits)
						{
							var pitArchive = release.PitArchives.FirstOrDefault(p => p.name == pit);
							if (pitArchive == null)
							{
								logger.Error("pitArchive is null. pit: {0}", pit);
								throw new ApplicationException(string.Format("pitArchive is null. pit: {0}", pit));
							}

							foreach (var filename in pitArchive.archives)
							{
								var archive = Path.Combine(release.basePath, 
									filename.Replace('/', Path.DirectorySeparatorChar));

								Debug.Assert(File.Exists(archive));

								outZip.Merge("pits", archive);
							}
						}
					}

					// Add users license file
					var licenseBytes = UTF8Encoding.UTF8.GetBytes((string)Session["LicenseXml"]);
					outZip.AddEntry("Peach.license", licenseBytes);

					outZip.Save(outFile);
					return outFile;
				}
			}
			catch
			{
				File.Delete(outFile);
				throw;
			}
		}
	}
}
