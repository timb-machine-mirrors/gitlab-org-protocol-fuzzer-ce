using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;

namespace Peach.Pro.Test.Core.Monitors
{
	[TestFixture] [Category("Peach")]
	class CleanupFolderMonitorTests
	{
		string tmp;
		string dir1;
		string file1;
		string dir1File;

		[SetUp]
		public void SetUp()
		{
			tmp = Path.GetTempFileName();
			dir1 = Path.Combine(tmp, "sub");
			file1 = Path.Combine(tmp, "file");
			dir1File = Path.Combine(dir1, "file");

			File.Delete(tmp);
			Directory.CreateDirectory(tmp);
			Directory.CreateDirectory(dir1);
			File.Create(file1).Close();
			File.Create(dir1File).Close();
		}

		[TearDown]
		public void TearDown()
		{
			Assert.True(Directory.Exists(tmp), "Temp directory '{0}' should exist".Fmt(tmp));
			Assert.True(Directory.Exists(dir1), "Directory '{0}' should exist".Fmt(dir1));
			Assert.True(File.Exists(file1), "File '{0}' should exist".Fmt(file1));
			Assert.True(File.Exists(dir1File), "File '{0}' should exist".Fmt(dir1File));

			Directory.Delete(tmp, true);
		}

		[Test]
		public void TestBadFolder()
		{
			// Should run even if the folder does not exist
			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", "some_unknown_filder" },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}

		[Test]
		public void TestNoNewFiles()
		{
			// Should not delete the folder being monotired or any files/directories that already exist

			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", tmp },
			});

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}

		[Test]
		public void TestPreserveExisting()
		{
			var dir2 = Path.Combine(tmp, "newsub");
			var file2 = Path.Combine(tmp, "newfile");
			var dir2File = Path.Combine(dir2, "newfile");

			var runner = new MonitorRunner("CleanupFolder", new Dictionary<string, string>
			{
				{ "Folder", tmp },
			});

			runner.IterationStarting += (m, args) =>
			{
				Directory.CreateDirectory(dir2);
				File.Create(file2).Close();
				File.Create(dir2File).Close();

				m.IterationStarting(args);

				Assert.False(Directory.Exists(dir2), "Directory '{0}' should not exist".Fmt(dir2));
				Assert.False(File.Exists(file2), "File '{0}' should not exist".Fmt(file2));
				Assert.False(File.Exists(dir2File), "File '{0}' should not exist".Fmt(dir2File));
			};

			var faults = runner.Run();

			Assert.AreEqual(0, faults.Length, "Monitor should produce no faults");
		}
	}
}
