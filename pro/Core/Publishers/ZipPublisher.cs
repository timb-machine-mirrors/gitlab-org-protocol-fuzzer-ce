using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using NLog;
using Ionic.Zip;
using Peach.Core.IO;
using System.Threading;

namespace Peach.Enterprise.Publishers
{
	[Publisher("Zip", true)]
	[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
	public class ZipPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string FileName { get; set; }

		private static int maxOpenAttempts = 10;

		private Stream fileStream;
		private ZipFile zipFile;

		public ZipPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			int i = 0;

			while (true)
			{
				try
				{
					fileStream = File.Open(FileName, FileMode.Create);
					zipFile = new ZipFile();
					return;
				}
				catch (Exception ex)
				{
					if (++i < maxOpenAttempts)
					{
						Thread.Sleep(200);
					}
					else
					{
						Logger.Error("Could not open file '{0}' after {1} attempts.  {2}", FileName, maxOpenAttempts, ex.Message);
						throw new SoftException(ex);
					}
				}
			}
		}

		protected override void OnClose()
		{
			try
			{
				if (zipFile != null)
					zipFile.Save(fileStream);
			}
			catch (Exception ex)
			{
				throw new SoftException("Zip publisher could not save file.", ex);
			}
			finally
			{
				if (zipFile != null)
				{
					zipFile.Dispose();
					zipFile = null;
				}

				if (fileStream != null)
				{
					fileStream.Dispose();
					fileStream = null;
				}
			}
		}

		protected override void OnOutput(BitwiseStream data)
		{
			// This publisher only supports output of data models
			throw new NotSupportedException();
		}

		public override void output(DataModel dataModel)
		{
			long cnt = 0;

			DataElement elem = dataModel;

			while (elem != null)
			{
				var stream = elem as Dom.Stream;
				if (stream == null)
				{
					var cont = elem as DataElementContainer;
					if (cont != null && cont.Count > 0)
					{
						elem = cont[0];
						continue;
					}
				}

				if (stream == null)
					throw new SoftException("Zip publisher expected a <Stream> element in the DataModel.");

				AddZipEntry(stream);
				++cnt;

				DataElement next;

				do
				{
					next = elem.nextSibling();
					elem = elem.parent;
				}
				while (next == null && elem != null);

				elem = next;
			}

			logger.Debug("Added {0} entries to zip file.", cnt);
		}

		private void AddZipEntry(Dom.Stream stream)
		{
			string entryName;
			BitwiseStream entryData;

			try
			{
				entryName = (string)stream["Name"].InternalValue;
			}
			catch (Exception ex)
			{
				throw new SoftException("Zip publisher could not get stream name.", ex);
			}

			try
			{
				entryData = stream["Content"].Value.PadBits();
			}
			catch (Exception ex)
			{
				throw new SoftException("Zip publisher could not get stream contents.", ex);
			}

			try
			{
				zipFile.AddEntry(entryName, entryData);
			}
			catch (Exception ex)
			{
				throw new SoftException("Zip publisher could not add entry to zip file.", ex);
			}
		}
	}
}
