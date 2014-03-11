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
	[Serializable]
	public class Dom : Peach.Core.Dom.Dom
	{
		public NamedCollection<GodelContext> godel = new NamedCollection<GodelContext>();
	}

	/// <summary>
	/// Extension of PitParser to add Ocl into the mix!
	/// </summary>
	public class GodelPitParser : PitParser
	{
		private class GodelNode : INamed
		{
			public GodelNode(Peach.Core.Dom.Action action, XmlNode node)
				: this("Action", string.Join(".", action.parent.parent.name, action.parent.name, action.name), node)
			{
			}

			public GodelNode(Peach.Core.Dom.State state, XmlNode node)
				: this("State", string.Join(".", state.parent.name, state.name), node)
			{
			}

			public GodelNode(Peach.Core.Dom.StateModel stateModel, XmlNode node)
				: this("StateModel", stateModel.name, node)
			{
			}

			public GodelNode(XmlNode node)
			{
				this.type = "top level";
				this.name = null;
				this.node = node;
			}

			private GodelNode(string type, string name, XmlNode node)
			{
				this.type = string.Format("{0} '{1}'", type, name);
				this.name = name;
				this.node = node;
			}

			public string type { get; private set; }
			public string name { get; private set; }
			public XmlNode node { get; private set; }

		}

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		NamedCollection<GodelNode> godelNodes;
		Dictionary<string, GodelContext> topLevel;

		public ExtendPeach ExtendPeach { get; set; }

		protected override Peach.Core.Dom.Dom getNewDom()
		{
			return new Dom();
		}

		protected override void handlePeach(Peach.Core.Dom.Dom dom, XmlNode node, Dictionary<string, object> args)
		{
			godelNodes = new NamedCollection<GodelNode>();
			topLevel = new Dictionary<string, GodelContext>();

			base.handlePeach(dom, node, args);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					var name = child.getAttrString("name");
					var godel = handleGodelNode(new GodelNode(child));

					try
					{
						topLevel.Add(name, godel);
					}
					catch (ArgumentException ex)
					{
						throw new PeachException("Error, a top level <Godel> element named '{0}' already exists.".Fmt(name), ex);
					}
				}
			}

			var godelDom = (Dom)dom;
			foreach (var item in godelNodes)
			{
				var godel = handleGodelNode(item);
				godelDom.godel.Add(godel);

				logger.Debug("Attached godel node to {0}.", item.type);
			}

			godelNodes = null;
			topLevel = null;
		}

		private GodelContext handleGodelNode(GodelNode node)
		{
			GodelContext godel;

			if (node.node.hasAttr("ref"))
			{
				string refName = node.node.getAttrString("ref");

				GodelContext other;
				if (!topLevel.TryGetValue(refName, out other))
					throw new PeachException("Error, could not resolve " + node.type + " <Godel> element ref attribute value '" + refName + "'.");

				godel = ObjectCopier.Clone(other);
			}
			else
			{
				godel = new GodelContext();
			}

			godel.debugName = node.type;
			godel.name = node.name;
			godel.controlOnly = node.node.getAttr("controlOnly", godel.controlOnly);
			godel.inv = node.node.getAttr("inv", godel.inv);
			godel.pre = node.node.getAttr("pre", godel.pre);
			godel.post = node.node.getAttr("post", godel.post);

			return godel;
		}

		private void deferParse(GodelNode node)
		{
			try
			{
				godelNodes.Add(node);
			}
			catch (ArgumentException ex)
			{
				throw new PeachException("Error, more than one <Godel> element specified on {0}.".Fmt(node.type), ex);
			}
		}

		protected override Peach.Core.Dom.Action handleAction(System.Xml.XmlNode node, Peach.Core.Dom.State parent)
		{
			var action = base.handleAction(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
					deferParse(new GodelNode(action, child));
			}

			return action;
		}

		protected override Peach.Core.Dom.State handleState(System.Xml.XmlNode node, Peach.Core.Dom.StateModel parent)
		{
			var state = base.handleState(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
					deferParse(new GodelNode(state, child));
			}

			return state;
		}

		protected override Peach.Core.Dom.StateModel handleStateModel(System.Xml.XmlNode node, Peach.Core.Dom.Dom parent)
		{
			var stateModel = base.handleStateModel(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
					deferParse(new GodelNode(stateModel, child));
			}

			return stateModel;
		}
	}
}
