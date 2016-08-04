using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NLog;

namespace PeachDownloader
{
	/// <summary>
	/// Send a binary file back to the user
	/// </summary>
	public partial class dl : System.Web.UI.Page
	{
		static Logger logger = LogManager.GetCurrentClassLogger();

		protected void Page_Load(object sender, EventArgs e)
		{
			if (!(bool)Session[SessionKeys.Authenticated])
			{
				logger.Error("Error 10001: Authenticated == false");

                Response.Write("Error: 10001");
				return;
			}

			if (!(bool)Session[SessionKeys.AcceptLicense])
			{
				logger.Error("Error 10002: AcceptLicense == false");
				Response.Write("Error: 10002");
				return;
			}

			Session[SessionKeys.AcceptLicense] = false;

			var downloads = Session[SessionKeys.Downloads] as SortedDictionary<string, SortedDictionary<string, JsonRelease>>;
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
				logger.Error("Error 10005: Unsupported release version");
				Response.Write("Error: 10005");

				return;
			}

			if (rel.Version == 2)
			{
				try
				{
					var dlFile = Path.Combine(rel.basePath, file);
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
			return release.files.Contains(file);
		}
	}
}
