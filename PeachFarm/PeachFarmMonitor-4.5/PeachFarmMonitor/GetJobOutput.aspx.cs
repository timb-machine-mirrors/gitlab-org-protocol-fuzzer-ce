using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PeachFarm.Common.Mongo;
using PeachFarm.Common;
using PeachFarmMonitor.Configuration;
using Ionic.Zip;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
//using System.IO.Compression;

namespace PeachFarmMonitor
{
  public partial class GetJobOutput : System.Web.UI.Page
  {
    private static PeachFarmMonitorSection monitorconfig = null;

		public static string[] ignoreextensions = new string[] { ".zip", ".pdf" };

    protected void Page_Load(object sender, EventArgs e)
    {
      if (!IsPostBack)
      {
        string jobID = Request.QueryString["jobid"];
        string filepath = Request.QueryString["file"];

        if (String.IsNullOrEmpty(filepath) == false && String.IsNullOrEmpty(jobID))
        {
          jobID = filepath.Substring(4, 12);
        }

        if (String.IsNullOrEmpty(jobID) == false)
        {
          monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
          
          Job job = DatabaseHelper.GetJob(jobID, monitorconfig.MongoDb.ConnectionString);
          if (job != null)
          {
            if (String.IsNullOrEmpty(filepath))
            {
							MongoServer server = new MongoClient(monitorconfig.MongoDb.ConnectionString).GetServer();
							MongoDatabase db = server.GetDatabase(MongoNames.Database);
							string jobfolder = job.JobFolder + "*";
							var query = Query.Matches("filename", new MongoDB.Bson.BsonRegularExpression(jobfolder, "i"));
							var files = db.GridFS.Find(query);

							Response.Clear();

							Response.BufferOutput = false;
							Response.ContentType = "application/zip";
							Response.AddHeader("Content-Disposition", "attachment; filename=" + job.JobFolder + ".zip");


							using (var zip = new ZipFile())
							{
								zip.CompressionLevel = Ionic.Zlib.CompressionLevel.None;
								zip.UseZip64WhenSaving = Zip64Option.Always;
								zip.BufferSize = 4 * 1024 * 1024;

								foreach (var file in files)
								{
									bool ignore = ((ignoreextensions != null) && (ignoreextensions.Contains(Path.GetExtension(file.Name))));
									if (!ignore)
									{
										if (zip.ContainsEntry(file.Name))
											continue;
										
										zip.AddEntry(file.Name, db.GridFS.OpenRead(file.Name));
									}
								}
								zip.Save(Response.OutputStream);
							}

						}
            else
            {
							if (DatabaseHelper.GridFSFileExists(filepath, monitorconfig.MongoDb.ConnectionString) == false)
							{
								filepath = filepath.Replace('\\','/');
							}

							string filename = Path.GetFileName(filepath);

							MongoServer server = new MongoClient(monitorconfig.MongoDb.ConnectionString).GetServer();
							MongoDatabase db = server.GetDatabase(MongoNames.Database);
							using (var stream = db.GridFS.OpenRead(filepath))
							{
								WriteStreamToResponse(stream, filename);
							}
							//server.Disconnect();
							
							
							
							

            }
          }
        }
      }
    }

		private void WriteStreamToResponse(Stream stream, string pathtobrowser, string contentType = "application/octet-stream")
		{
			//System.IO.Stream iStream = null;

			// Buffer to read 10K bytes in chunk:
			byte[] buffer = new Byte[10000];

			// Length of the file:
			int length;

			// Total bytes to read:
			long dataToRead;

			// Identify the file to download including its path.
			//string filepath = "DownloadFileName";

			// Identify the file name.
			//string filename = System.IO.Path.GetFileName(filepath);

			try
			{
				// Open the file.
				//iStream = new System.IO.FileStream(filepath, System.IO.FileMode.Open,
				//			System.IO.FileAccess.Read, System.IO.FileShare.Read);


				// Total bytes to read:
				dataToRead = stream.Length;

				Response.ContentType = contentType;
				Response.AddHeader("Content-Disposition", "attachment; filename=" + pathtobrowser);

				// Read the bytes.
				while (dataToRead > 0)
				{
					// Verify that the client is connected.
					if (Response.IsClientConnected)
					{
						// Read the data in buffer.
						length = stream.Read(buffer, 0, 10000);

						// Write the data to the current output stream.
						Response.OutputStream.Write(buffer, 0, length);

						// Flush the data to the HTML output.
						Response.Flush();

						buffer = new Byte[10000];
						dataToRead = dataToRead - length;
					}
					else
					{
						//prevent infinite loop if user disconnects
						dataToRead = -1;
					}
				}
			}
			catch (Exception ex)
			{
				// Trap the error, if any.
				Response.Write("Error : " + ex.Message);
			}
			finally
			{
				//if (stream != null)
				//{
				//	//Close the file.
				//	stream.Close();
				//}
				//Response.Close();
			}
		}

		private void WriteFileToResponse(string filepath, string pathtobrowser, string contentType = "application/octet-stream")
		{
			System.IO.Stream iStream = null;

			// Buffer to read 10K bytes in chunk:
			byte[] buffer = new Byte[10000];

			// Length of the file:
			int length;

			// Total bytes to read:
			long dataToRead;

			// Identify the file to download including its path.
			//string filepath = "DownloadFileName";

			try
			{
				// Open the file.
				iStream = new System.IO.FileStream(filepath, System.IO.FileMode.Open,
							System.IO.FileAccess.Read, System.IO.FileShare.Read);


				// Total bytes to read:
				dataToRead = iStream.Length;

				Response.ContentType = contentType;
				Response.AddHeader("Content-Disposition", "attachment; filename=" + pathtobrowser);

				// Read the bytes.
				while (dataToRead > 0)
				{
					// Verify that the client is connected.
					if (Response.IsClientConnected)
					{
						// Read the data in buffer.
						length = iStream.Read(buffer, 0, 10000);

						// Write the data to the current output stream.
						Response.OutputStream.Write(buffer, 0, length);

						// Flush the data to the HTML output.
						Response.Flush();

						buffer = new Byte[10000];
						dataToRead = dataToRead - length;
					}
					else
					{
						//prevent infinite loop if user disconnects
						dataToRead = -1;
					}
				}
			}
			catch (Exception ex)
			{
				// Trap the error, if any.
				Response.Write("Error : " + ex.Message);
			}
			finally
			{
				if (iStream != null)
				{
					//Close the file.
					iStream.Close();
				}
				//Response.Close();
			}
		}
  }
}