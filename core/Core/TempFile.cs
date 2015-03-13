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
}
