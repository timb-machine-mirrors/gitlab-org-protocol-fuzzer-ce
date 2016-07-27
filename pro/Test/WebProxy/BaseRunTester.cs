using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Cracker;
using Peach.Core.Dom;
using Peach.Core.IO;
using Peach.Core.Test;
using Peach.Pro.Core;
using Peach.Pro.Core.License;
using Peach.Pro.Core.Runtime;
using Peach.Pro.Core.WebApi;
using Peach.Pro.Core.WebApi.Proxy;
using Peach.Pro.Test.Core.PitParserTests;
using Peach.Pro.Test.WebProxy.TestTarget;
using Titanium.Web.Proxy.EventArguments;
using Action = Peach.Core.Dom.Action;
using Encoding = System.Text.Encoding;

namespace Peach.Pro.Test.WebProxy
{
	public class BaseRunTester : Watcher
	{
		public static string BaseUrl = "http://localhost.:8002";
		protected IDisposable _server;
		protected string SwaggerFile = null;

		/// <summary>
		/// Get an instance of HTTP Client
		/// </summary>
		/// <returns></returns>
		public HttpClient GetHttpClient()
		{
			var cookies = new CookieContainer();
			var handler = new HttpClientHandler
			{
				CookieContainer = cookies,
				UseCookies = true,
				UseDefaultCredentials = false,
				Proxy = new System.Net.WebProxy("http://127.0.0.1:8001", false, new string[] { }),
				UseProxy = true,
			};

			return new HttpClient(handler);
		}

		public static string GetValuesJson()
		{
			var assembly = Assembly.GetExecutingAssembly();
			using (var textStream =
				new StreamReader(
					assembly.GetManifestResourceStream(
						"Peach.Pro.Test.WebProxy.TestTarget.SwaggerValuesApi.json")))
			{
				return textStream.ReadToEnd();
			}
		}


		public string StreamVariantToString(Variant v)
		{
			var stream = (BitStream)v;
			stream.Position = 0;

			var reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}

		[SetUp]
		public virtual void Setup()
		{
			cloneActions = false;
			ResetContainers();
		}

		[OneTimeSetUp]
		public virtual void Init()
		{
			BaseProgram.Initialize();
			_server = TestTargetServer.StartServer();

			SwaggerFile = Path.GetTempFileName();
			File.WriteAllText(SwaggerFile, GetValuesJson());
		}

		[OneTimeTearDown]
		public virtual void Cleanup()
		{
			if (_server != null)
				_server.Dispose();

			if (SwaggerFile != null)
				File.Delete(SwaggerFile);

			_server = null;
		}

		#region From DataModelCollector

		public static Dom ParsePit(string xml, Dictionary<string, object> args = null)
		{
			var featureName = "PeachPit-Net-DNP3_Slave";

			var license = new Mock<ILicense>();
			license.Setup(x => x.GetFeature(featureName))
				   .Returns(() => null);

			return new ProPitParser(license.Object, "", "").asParser(args, new MemoryStream(Encoding.UTF8.GetBytes(xml)));
		}

		public static void VerifyRoundTrip(string xml)
		{
			// Given a data model snippet, we should be able to
			// 1) Parse it
			// 2) Get its default value
			// 3) Crack the default value
			// 4) Get its new default value
			// 5) Expect values to be identical

			var dom = ParsePit(xml);

			Assert.AreEqual(1, dom.dataModels.Count);

			var expected = dom.dataModels[0].Value.ToArray();
			var bs = new BitStream(expected);
			var cracker = new DataCracker();

			cracker.CrackData(dom.dataModels[0], bs);

			var actual = dom.dataModels[0].Value.ToArray();

			Assert.AreEqual(expected, actual);
		}

		protected void RunEngine(string xml, bool singleIteration = false)
		{
			RunEngine(ParsePit(xml), singleIteration);
		}

		protected void RunEngine(Dom dom, bool singleIteration)
		{
			var e = new Engine(this);

			var cfg = new RunConfiguration();
			if (singleIteration)
				cfg.singleIteration = true;

			this.dom = dom;
			e.startFuzzing(dom, cfg);
		}

		protected void RunEngine(string xml, string pitFilename)
		{
			RunEngine(ParsePit(xml), pitFilename);
		}

		protected Dom dom;

		protected void RunEngine(Dom dom, string pitFilename)
		{
			var e = new Engine(this);
			var cfg = new RunConfiguration();
			cfg.pitFile = pitFilename;

			this.dom = dom;
			e.startFuzzing(dom, cfg);
		}

		protected List<Variant> mutations = null;
		protected List<BitwiseStream> values = null;
		protected List<DataModel> dataModels = null;
		protected List<DataModel> mutatedDataModels = null;
		protected List<Action> actions = null;
		protected List<string> strategies = null;
		protected List<string> iterStrategies = null;
		protected List<string> allStrategies = null;
		protected bool cloneActions = false;

		protected void ResetContainers()
		{
			values = new List<BitwiseStream>();
			mutations = new List<Variant>();
			actions = new List<Action>();
			dataModels = new List<DataModel>();
			mutatedDataModels = new List<DataModel>();
			strategies = new List<string>();
			allStrategies = new List<string>();
			iterStrategies = new List<string>();
		}

		protected override void ActionFinished(RunContext context, Action action)
		{
			if (!action.allData.Any())
				return;

			var dom = action.parent.parent.parent as Dom;

			foreach (var item in action.allData)
			{
				SaveDataModel(dom, item.dataModel);
			}

			if (cloneActions)
				actions.Add(ObjectCopier.Clone(action));
			else
				actions.Add(action);
		}

		void SaveDataModel(Dom dom, DataModel model)
		{
			// Collect mutated values only after the first run
			if (!dom.context.controlIteration)
			{
				mutations.Add(model.Count > 0 ? model[0].InternalValue : null);
				mutatedDataModels.Add(model);
			}

			// Collect transformed values, actions and dataModels always
			values.Add(model.Count > 0 ? model[0].Value : null);
			dataModels.Add(model);
		}

		protected override void DataMutating(RunContext context, ActionData actionData, DataElement element, Mutator mutator)
		{
			int len = strategies.Count;
			string item = mutator.Name + " | " + element.fullName;
			allStrategies.Add(item);
			if (len == 0 || strategies[len - 1] != item)
				strategies.Add(item);

			while (iterStrategies.Count < (actions.Count + 1))
				iterStrategies.Add("");

			if (iterStrategies[actions.Count].Length > 0)
				iterStrategies[actions.Count] += " ; ";

			iterStrategies[actions.Count] += item;
		}

		#endregion
	}
}
