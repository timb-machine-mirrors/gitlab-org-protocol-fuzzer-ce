using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using System.Xml;
using System.Xml.Schema;
using NLog;
using Godel.Core.OCL;

namespace Godel.Core
{
	/// <summary>
	/// Extension of PitParser to add Ocl into the mix!
	/// </summary>
	public class GodelPitParser : PitParser
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public Dictionary<string, OCL.OclContext> OclContextsInstances { get; set; }
		public ExtendPeach ExtendPeach { get; set; }

		public bool ControlOnlyOclContexts { get; set; }

		protected override void handlePeach(Dom dom, XmlNode node, Dictionary<string, object> args)
		{
			base.handlePeach(dom, node, args);

			Dom newDom = dom;

			OclContextsInstances = (Dictionary<string, OCL.OclContext>)this.ExtendPeach.Context.stateStore["OclContexts"];
			newDom.context = this.ExtendPeach.Context;

			foreach (XmlNode child in node)
			{
				if (child.Name == "Ocl")
				{
					ControlOnlyOclContexts = false;

					if (child.hasAttribute("controlOnly"))
						ControlOnlyOclContexts = child.getAttrBool("controlOnly");

					handleOcl(child);
				}
			}
		}

		protected void handleOcl(XmlNode node)
		{
			string ocl = null;

			foreach (XmlNode child in node.ChildNodes)
			{
				if (child.NodeType == XmlNodeType.CDATA)
				{
					ocl = child.InnerText.Trim();
				}
			}

			if (ocl == null)
				throw new PeachException("Error, did not locate any OCL inside of Ocl element.  Make sure your using correct cdata syntax!");

			// Compile ocl
			List<OclContext> contexts;

			try
			{
				contexts = Ocl.ParseOcl(ocl);
			}
			catch (Exception ex)
			{
				throw new PeachException("Error, the OCL script provided did not compile: " + ex.Message);
			}

			foreach (var context in contexts)
			{
				logger.Debug("handleOcl: Adding new context: " + context.Name);

				if (OclContextsInstances.ContainsKey(context.Name))
					throw new PeachException("Error, duplicate OCL context specified named '" + context.Name + "'.");

				OclContextsInstances[context.Name] = context;
			}
		}

		protected override Peach.Core.Dom.Action handleAction(System.Xml.XmlNode node, Peach.Core.Dom.State parent)
		{
			Peach.Core.Dom.Action action = base.handleAction(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Ocl")
				{
					if (!child.hasAttribute("context"))
						throw new PeachException("Error, Action->Ocl element must contain a 'context' attribute.");

					this.ExtendPeach.ExtendAction.AttachToAction(action, child.getAttribute("context"));
				}
			}

			return action;
		}

		protected override Peach.Core.Dom.State handleState(System.Xml.XmlNode node, Peach.Core.Dom.StateModel parent)
		{
			State state = base.handleState(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Ocl")
				{
					if (!child.hasAttribute("context"))
						throw new PeachException("Error, State->Ocl element must contain a 'context' attribute.");

					this.ExtendPeach.ExtendState.AttachToState(state, child.getAttribute("context"));
				}
			}

			return state;
		}

		protected override Peach.Core.Dom.StateModel handleStateModel(System.Xml.XmlNode node, Peach.Core.Dom.Dom parent)
		{
			StateModel stateModel = base.handleStateModel(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Ocl")
				{
					if (!child.hasAttribute("context"))
						throw new PeachException("Error, StateModel->Ocl element must contain a 'context' attribute.");

					this.ExtendPeach.ExtendStateModel.AttachToStateModel(stateModel, child.getAttribute("context"));
				}
			}

			return stateModel;
		}
	}
}
