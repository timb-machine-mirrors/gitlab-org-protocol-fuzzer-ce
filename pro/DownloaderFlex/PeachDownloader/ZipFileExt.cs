using System.IO;
using Ionic.Zip;

namespace PeachDownloader
{
	public static class ZipFileExt
	{
		public static ZipFile Merge(this ZipFile item, string fileName)
		{
			using(var zip = new ZipFile(fileName))
			{
				return item.Merge(zip);
			}
		}
		
		public static ZipFile Merge(this ZipFile item, ZipFile file)
		{
			foreach (var entry in file)
				item.AddEntry(entry.FileName, entry.ExtractAsStream());

			return item;
		}

		public static ZipFile Merge(this ZipFile item, string pathPrefix, string fileName)
		{
			using(var zip = new ZipFile(fileName))
			{
				return item.Merge(pathPrefix, zip);
			}
		}

		public static ZipFile Merge(this ZipFile item, string pathPrefix, ZipFile file)
		{
			//if(!item.ContainsEntry(pathPrefix))
			//	item.AddDirectory(pathPrefix);

			foreach (var entry in file)
			{
				var fileName = Path.Combine(pathPrefix, entry.FileName);

				// Skip existing files
				if(!item.ContainsEntry(fileName))
					item.AddEntry(fileName, entry.ExtractAsStream());
			}

			return item;
		}
		
		public static MemoryStream ExtractAsStream(this ZipEntry entry)
		{
			var ms = new MemoryStream();
			entry.Extract(ms);
			ms.Position = 0;

			return ms;
		}
	}
}