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

	[Serializable]
	public class StateModel : Peach.Core.Dom.StateModel
	{
		[NonSerialized]
		public NamedCollection<GodelContext> godel = new NamedCollection<GodelContext>();
	}

	/// <summary>
	/// Extension of PitParser to add Ocl into the mix!
	/// </summary>
	public class GodelPitParser : PitParser
	{
		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		protected override Peach.Core.Dom.Dom CreateDom()
		{
			return new Dom();
		}

		protected override Peach.Core.Dom.StateModel CreateStateModel()
		{
			return new StateModel();
		}

		protected override void handlePeach(Peach.Core.Dom.Dom dom, XmlNode node, Dictionary<string, object> args)
		{
			var godelDom = (Dom)dom;

			base.handlePeach(dom, node, args);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					GodelContext godel;

					var refName = child.getAttr("ref", null);
					if (refName != null)
					{
						var other = godelDom.getRef<GodelContext>(refName, d => ((Dom)d).godel);
						if (other == null)
							throw new PeachException("Error, could not resolve top level <Godel> element ref attribute value '" + refName + "'.");

						godel = ObjectCopier.Clone(other);
					}
					else
					{
						godel = new GodelContext();
					}

					godel.name = child.getAttrString("name");
					godel.refName = child.getAttr("ref", godel.refName);
					godel.controlOnly = child.getAttr("controlOnly", godel.controlOnly.GetValueOrDefault());
					godel.inv = child.getAttr("inv", godel.inv);
					godel.pre = child.getAttr("pre", godel.pre);
					godel.post = child.getAttr("post", godel.post);

					try
					{
						godelDom.godel.Add(godel);
					}
					catch (ArgumentException ex)
					{
						throw new PeachException("Error, a top level <Godel> element named '{0}' already exists.".Fmt(godel.name), ex);
					}
				}
			}

			foreach (StateModel sm in godelDom.stateModels)
			{
				foreach (var item in sm.godel)
				{
					if (!string.IsNullOrEmpty(item.refName))
					{
						var other = godelDom.getRef<GodelContext>(item.refName, d => ((Dom)d).godel);
						if (other == null)
							throw new PeachException("Error, could not resolve " + item.debugName + " <Godel> element ref attribute value '" + item.refName + "'.");

						item.inv = item.inv ?? other.inv;
						item.pre = item.pre ?? other.pre;
						item.post = item.post ?? other.post;

						if (!item.controlOnly.HasValue)
							item.controlOnly = other.controlOnly;
					}
					else
					{
						if (!item.controlOnly.HasValue)
							item.controlOnly = false;
					}

					logger.Debug("Attached godel node to {0}.", item.debugName);
				}
			}

			// The PitParser changes the state model name to include the namespace
			// when parsing <Test> so we need to update the names of our godel nodes.
			foreach (var test in godelDom.tests)
			{
				var newList = new NamedCollection<GodelContext>();
				var sm = (StateModel)test.stateModel;

				foreach (var item in sm.godel)
				{
					var idx = item.name.IndexOf('.');
					if (idx < 0)
						item.name = sm.name;
					else
						item.name = sm.name + item.name.Substring(item.name.IndexOf('.'));

					item.debugName = "{0} '{1}'".Fmt(item.type, item.name);

					newList.Add(item);
				}

				// If the test uses a state model that has godel nodes,
				// add the godel logger to the test.
				if (newList.Count > 0)
					test.loggers.Insert(0, new GodelLogger());

				sm.godel = newList;
			}
		}

		private void deferParse(StateModel sm, string fullName, XmlNode node)
		{
			var godel = new GodelContext()
			{
				debugName = "{0} '{1}'".Fmt(node.ParentNode.Name, fullName),
				type = node.ParentNode.Name,
				name = fullName,
				refName = node.getAttr("ref", null),
				inv = node.getAttr("inv", null),
				pre = node.getAttr("pre", null),
				post = node.getAttr("post", null),
			};

			if (node.hasAttr("controlOnly"))
				godel.controlOnly = node.getAttr("controlOnly", false);

			try
			{
				sm.godel.Add(godel);
			}
			catch (ArgumentException ex)
			{
				throw new PeachException("Error, more than one <Godel> element specified on {0}.".Fmt(godel.debugName), ex);
			}
		}

		protected override Peach.Core.Dom.Action handleAction(System.Xml.XmlNode node, Peach.Core.Dom.State parent)
		{
			var action = base.handleAction(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					var fullName = string.Join(".", action.parent.parent.name, action.parent.name, action.name);
					deferParse((StateModel)parent.parent, fullName, child);
				}
			}

			return action;
		}

		protected override Peach.Core.Dom.State handleState(System.Xml.XmlNode node, Peach.Core.Dom.StateModel parent)
		{
			var state = base.handleState(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					var fullName = string.Join(".", state.parent.name, state.name);
					deferParse((StateModel)parent, fullName, child);
				}
			}

			return state;
		}

		protected override Peach.Core.Dom.StateModel handleStateModel(System.Xml.XmlNode node, Peach.Core.Dom.Dom parent)
		{
			var stateModel = base.handleStateModel(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
					deferParse((StateModel)stateModel, stateModel.name, child);
			}

			return stateModel;
		}
	}
}
