using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.WebServices;
using Peach.Pro.Core.WebServices.Models;
using Peach.Pro.WebApi2;

namespace Peach.Pro.Test.WebApi
{
	[SetUpFixture]
	internal class TestBase : SetUpFixture
	{
		[OneTimeSetUp]
		public void SetUp()
		{
			DoSetUp();
		}

		[OneTimeTearDown]
		public void TearDown()
		{
			DoTearDown();
		}
	}

	[TestFixture]
	[Quick]
	internal class CommonTests : TestFixture
	{
		public CommonTests()
			: base(Assembly.GetExecutingAssembly())
		{
		}

		[Test]
		public void AssertWorks()
		{
			DoAssertWorks();
		}

		[Test]
		public void NoMissingAttributes()
		{
			DoNoMissingAttributes();
		}
	}

	abstract class ControllerTestsBase
	{
		protected class TestJobMonitor : IJobMonitor
		{
			readonly int _pid = Utilities.GetCurrentProcessId();

			public void Dispose()
			{
			}

			public int Pid { get { return _pid; } }

			public bool IsTracking(Job job)
			{
				lock (this)
				{
					return RunningJob != null && RunningJob.Guid == job.Guid;
				}
			}

			public bool IsControllable { get { return true; } }

			public Job GetJob()
			{
				return RunningJob;
			}

			public Job RunningJob { get; set; }

			public Job Start(string pitLibraryPath, string pitFile, JobRequest jobRequest)
			{
				throw new NotImplementedException();
			}

			public bool Pause()
			{
				throw new NotImplementedException();
			}

			public bool Continue()
			{
				throw new NotImplementedException();
			}

			public bool Stop()
			{
				throw new NotImplementedException();
			}

			public bool Kill()
			{
				throw new NotImplementedException();
			}

			public EventHandler InternalEvent { set { } }
		}

		protected TempDirectory _tmpDir;
		protected HttpServer _server;
		protected HttpMessageInvoker _client;
		protected WebContext _context;
		protected Mock<ILicense> _license = new Mock<ILicense>();
		protected Mock<IPitDatabase> _pitDatabase = new Mock<IPitDatabase>();
		protected Mock<IJobMonitor> _jobMonitor = new Mock<IJobMonitor>();

		public ControllerTestsBase()
		{
			_license = new Mock<ILicense>();
		}

		public void DoSetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;

			_context = new WebContext(Path.Combine(_tmpDir.Path, "pits"));

			var config = WebServer.CreateHttpConfiguration(
				_context, 
				_license.Object,
				_pitDatabase.Object,
				_jobMonitor.Object
			);

			_server = new HttpServer(config);
			_client = new HttpMessageInvoker(_server);
		}

		public void DoTearDown()
		{
			_client.Dispose();
			_server.Dispose();
			_tmpDir.Dispose();
		}
	}
}
