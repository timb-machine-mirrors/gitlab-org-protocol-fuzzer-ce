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
		protected TempDirectory _tmpDir;
		protected HttpServer _server;
		protected HttpMessageInvoker _client;
		protected WebContext _context;
		protected Mock<ILicense> _license = new Mock<ILicense>();
		protected Mock<IPitDatabase> _pitDatabase = new Mock<IPitDatabase>();
		protected Mock<IJobMonitor> _jobMonitor = new Mock<IJobMonitor>();

		public void DoSetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;

			_context = new WebContext(Path.Combine(_tmpDir.Path, "pits"));

			var config = WebServer.CreateHttpConfiguration(
				_context, 
				_license.Object,
				_jobMonitor.Object,
				() => _pitDatabase.Object
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
