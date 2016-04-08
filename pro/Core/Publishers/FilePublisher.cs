
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@dejavusecurity.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NLog;
using Peach.Core;
using Peach.Core.IO;

namespace Peach.Pro.Core.Publishers
{
	[Publisher("File")]
	[Alias("FileStream")]
	[Alias("file.FileWriter")]
	[Alias("file.FileReader")]
	[Parameter("FileName", typeof(string), "Name of file to open for reading/writing")]
	[Parameter("Overwrite", typeof(bool), "Replace existing file? [true/false, default true]", "true")]
	[Parameter("Append", typeof(bool), "Append to end of file [true/false, default flase]", "false")]
	public class FilePublisher : Peach.Core.Publishers.StreamPublisher
	{
		private static NLog.Logger logger = LogManager.GetCurrentClassLogger();
		protected override NLog.Logger Logger { get { return logger; } }

		public string FileName { get; protected set; }
		public bool Overwrite { get; protected set; }
		public bool Append { get; protected set; }

		private const int maxOpenAttempts = 10;
		private readonly FileMode fileMode;

		public FilePublisher(Dictionary<string, Variant> args)
			: base(args)
		{
			if (Overwrite && Append)
				throw new PeachException("File publisher does not support Overwrite and Append being enabled at once.");

			if (Overwrite)
				fileMode = FileMode.Create;
			else if (Append)
				fileMode = FileMode.OpenOrCreate | FileMode.Append;
			else
				fileMode = FileMode.OpenOrCreate;
		}

		protected override void OnOpen()
		{
			Debug.Assert(stream == null);

			var dir = Path.GetDirectoryName(FileName);
			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			try
			{
				Retry.Execute(() =>
				{
					stream = File.Open(FileName, fileMode);
				}, TimeSpan.FromMilliseconds(200), maxOpenAttempts);
			}
			catch (Exception ex)
			{
				Logger.Error("Could not open file '{0}' after {1} attempts.  {2}", FileName, maxOpenAttempts, ex.Message);
				throw new SoftException(ex);
			}
		}

		protected override void OnClose()
		{
			Debug.Assert(stream != null);

			try
			{
				stream.Close();
			}
			catch (Exception ex)
			{
				logger.Error(ex.Message);
			}

			stream = null;
		}

		protected override void OnOutput(BitwiseStream data)
		{
			data.CopyTo(stream, BitwiseStream.BlockCopySize);
		}
	}
}
