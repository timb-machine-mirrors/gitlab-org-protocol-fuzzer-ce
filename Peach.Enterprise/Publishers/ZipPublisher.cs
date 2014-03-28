using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Peach.Core;
using Peach.Core.Dom;
using NLog;
using Ionic.Zip;
using Peach.Core.IO;

namespace Peach.Enterprise.Publishers
{
	[Publisher("Zip", true)]
	[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
	public class ZipPublisher : Publisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string FileName { get; set; }

		private Stream fileStream;
		private ZipFile zipFile;

		public ZipPublisher(Dictionary<string, Variant> args)
			: base(args)
		{
		}

		protected override void OnOpen()
		{
			fileStream = File.Open(FileName, FileMode.Create);
			zipFile = new ZipFile();
		}

		protected override void OnClose()
		{
			zipFile.Save(fileStream);

			zipFile.Dispose();
			zipFile = null;

			fileStream.Dispose();
			fileStream = null;
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
				throw new PeachException("Zip publisher could not get stream name.", ex);
			}

			try
			{
				entryData = PadBits(stream["Content"].Value);
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
