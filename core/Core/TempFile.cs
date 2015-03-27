using System;
using System.IO;

namespace Peach.Core
{
	public class TempFile : IDisposable
	{
		public string Path { get; private set; }

		public TempFile()
		{
			Path = System.IO.Path.GetTempFileName();
		}

		public void Dispose()
		{
			try { File.Delete(Path); }
			catch { }
		}
	}

	public class TempDirectory : IDisposable
	{
		public string Path { get; private set; }

		public TempDirectory()
		{
			Path = System.IO.Path.GetTempFileName();
			File.Delete(Path);
			Directory.CreateDirectory(Path);
		}

		public void Dispose()
		{
			try { Directory.Delete(Path, true); }
			catch { }
		}
	}
}
