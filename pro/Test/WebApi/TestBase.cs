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
using Peach.Pro.Core.License;
using Peach.Pro.Core.Runtime;
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

			// Peach.Core.dll
			ClassLoader.LoadAssembly(typeof(ClassLoader).Assembly);

			// Peach.Pro.dll
			ClassLoader.LoadAssembly(typeof(BaseProgram).Assembly);
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

		protected Mock<ILicense> _license;
		protected Mock<IPitDatabase> _pitDatabase;
		protected Mock<IJobMonitor> _jobMonitor;

		protected virtual Mock<ILicense> CreateLicense()
		{
			return new Mock<ILicense>();
		}

		protected virtual Mock<IPitDatabase> CreatePitDatabase()
		{
			return new Mock<IPitDatabase>();
		}

		protected virtual Mock<IJobMonitor> CreateJobMonitor()
		{
			return new Mock<IJobMonitor>();
		}

		[SetUp]
		public virtual void SetUp()
		{
			_tmpDir = new TempDirectory();

			Configuration.LogRoot = _tmpDir.Path;

			_context = new WebContext(Path.Combine(_tmpDir.Path, "pits"));
			_license = CreateLicense();
			_pitDatabase = CreatePitDatabase();
			_jobMonitor = CreateJobMonitor();

			var tuple = WebServer.CreateHttpConfiguration(
				_context, 
				_license.Object,
				_jobMonitor.Object,
				ctx => _pitDatabase.Object
			);

			var config = tuple.Item1;
			var container = tuple.Item2;

			_server = new HttpServer(config);
			_client = new HttpMessageInvoker(_server);
		}

		[TearDown]
		public virtual void TearDown()
		{
			_client.Dispose();
			_server.Dispose();
			_tmpDir.Dispose();
		}
	}
}
