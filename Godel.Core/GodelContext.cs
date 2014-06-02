using System;
using System.Linq;
using Peach.Core.Dom;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Collections.Generic;
using Peach.Core;
using NLog;

namespace Godel.Core
{
	[Serializable]
	public class GodelContext : INamed
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public string debugName { get; set; }
		public string type { get; set; }
		public string name { get; set; }
		public string refName { get; set; }
		public bool? controlOnly { get; set; }
		public string inv { get; set; }
		public string pre { get; set; }
		public string post { get; set; }

		private RunContext context;
		private ScriptScope globalScope;
		private CompiledCode invScript;
		private CompiledCode preScript;
		private CompiledCode postScript;

		public void Pre(object self)
		{
			Run(invScript, "pre-inv", self, null);
			Run(preScript, "pre", self, null);
		}

		public void Post(object self, object pre)
		{
			Run(invScript, "post-inv", self, null);
			Run(postScript, "post", self, pre);
		}

		public void OnTestStarting(RunContext context, ScriptScope globalScope)
		{
			this.context = context;
			this.globalScope = globalScope;

			invScript = Compile("inv", inv);
			preScript = Compile("pre", pre);
			postScript = Compile("post", post);
		}

		public void OnTestFinished()
		{
			context = null;
			globalScope = null;
			invScript = null;
			preScript = null;
			postScript = null;
		}

		CompiledCode Compile(string dir, string eval)
		{
			if (string.IsNullOrEmpty(eval))
				return null;

			try
			{
				var source = globalScope.Engine.CreateScriptSourceFromString(eval, SourceCodeKind.Expression);
				var ret = source.Compile();

				return ret;
			}
			catch (Exception ex)
			{
				var err = "Error compiling Godel {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new PeachException(err, ex);
			}
		}

		void Run(CompiledCode code, string dir, object self, object pre)
		{
			if (code == null)
				return;

			if (controlOnly.GetValueOrDefault() && !context.controlIteration)
			{
				logger.Debug("Godel {0}: Ignoring control only. ({1})", dir, debugName);
				return;
			}

			globalScope.SetVariable("self", self);
			globalScope.SetVariable("context", context);

			if (pre == null)
				globalScope.RemoveVariable("pre");
			else
				globalScope.SetVariable("pre", pre);

			object ret;

			try
			{
				ret = code.Execute(globalScope);
			}
			catch (Exception ex)
			{
				var err = "Error, Godel failed to execute {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new SoftException(err, ex);
			}

			bool bRet;

			try
			{
				bRet = (bool)ret;
			}
			catch (Exception ex)
			{
				var err = "Error, Godel failed to parse the return value for the {0} expression for {1}. {2}".Fmt(dir, debugName, ex.Message);
				throw new SoftException(err, ex);
			}

			if (bRet)
			{
				logger.Debug("Godel {0}: Passed. ({1})", dir, debugName);
				return;
			}

			var msg = "Godel {0} expression for {1} failed.".Fmt(dir, debugName);

			logger.Error(msg);

			Fault fault = new Fault();
			fault.detectionSource = "Godel";
			fault.type = FaultType.Fault;
			fault.title = msg;
			fault.description = msg;
			fault.folderName = "Godel";

			context.faults.Add(fault);

			throw new SoftException(msg);
		}
	}
}
