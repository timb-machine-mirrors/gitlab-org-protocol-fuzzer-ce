﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NLog;
using Peach.Core;
using Peach.Core.Analyzers;
using Peach.Core.Dom;
using Peach.Pro.Core.Dom.Actions;
using Peach.Pro.Core.Dom.Actions.Web;
using Peach.Pro.Core.Godel;
using Peach.Pro.Core.License;
using Peach.Pro.Core.WebApi;
using Action = Peach.Core.Dom.Action;
using Logger = NLog.Logger;
using StateModel = Peach.Pro.Core.Godel.StateModel;

namespace Peach.Pro.Core
{
	/// <summary>
	/// Extension of PitParser to add Ocl into the mix!
	/// </summary>
	public class ProPitParser : PitParser
	{
		public static readonly Logger logger = LogManager.GetCurrentClassLogger();

		private readonly IPitResource _pitResource;

		public ProPitParser(
			ILicense license,
			string pitLibraryPath,
			string pitPath,
			ResourceRoot root = null)
		{
			_pitResource = new PitResource(license, pitLibraryPath, pitPath, root);
		}

		// should only be used for unit tests
		internal ProPitParser()
		{
			_pitResource = new FilePitResource();
		}

		protected override Peach.Core.Dom.Dom CreateDom()
		{
			return new Godel.Dom();
		}

		protected override Peach.Core.Dom.StateModel CreateStateModel()
		{
			return new StateModel();
		}

		protected override void handlePeach(Peach.Core.Dom.Dom dom, XmlNode node, Dictionary<string, object> args)
		{
			var godelDom = (Godel.Dom)dom;

			base.handlePeach(dom, node, args);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					GodelContext godel;

					var refName = child.getAttr("ref", null);
					if (refName != null)
					{
						var other = godelDom.getRef(refName, d => ((Godel.Dom)d).godel);
						if (other == null)
							throw new PeachException("Error, could not resolve top level <Godel> element ref attribute value '" + refName + "'.");

						godel = ObjectCopier.Clone(other);
					}
					else
					{
						godel = new GodelContext();
					}

					godel.Name = child.getAttrString("name");
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
						throw new PeachException("Error, a top level <Godel> element named '{0}' already exists.".Fmt(godel.Name), ex);
					}
				}
			}

			foreach (var stateModel in godelDom.stateModels)
			{
				var sm = (StateModel)stateModel;
				foreach (var item in sm.godel)
				{
					if (!string.IsNullOrEmpty(item.refName))
					{
						var other = godelDom.getRef(item.refName, d => ((Godel.Dom)d).godel);
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
					var idx = item.Name.IndexOf('.');
					if (idx < 0)
						item.Name = sm.Name;
					else
						item.Name = sm.Name + item.Name.Substring(item.Name.IndexOf('.'));

					item.debugName = "{0} '{1}'".Fmt(item.type, item.Name);

					newList.Add(item);
				}

				// If the test uses a state model that has godel nodes,
				// add the godel logger to the test.
				if (newList.Count > 0)
					test.loggers.Insert(0, new GodelLogger());

				sm.godel = newList;
			}
		}

		protected override void handleInclude(Peach.Core.Dom.Dom dom, Dictionary<string, object> args, XmlNode child)
		{
			var ns = child.getAttrString("ns");
			var src = child.getAttrString("src")
				.Replace("file:", "");

			var stream = _pitResource.Load(src);
			if (stream == null)
				throw new PeachException("Error, Unable to locate Pit file [{0}].\n".Fmt(src));

			Peach.Core.Dom.Dom newDom;
			using (stream)
			{
				var dataName = Path.GetFileNameWithoutExtension(src);
				newDom = asParser(args, new StreamReader(stream), dataName, true);
				newDom.fileName = src;
			}

			newDom.Name = ns;
			dom.ns.Add(newDom);

			foreach (var item in newDom.Python.Paths)
				dom.Python.AddSearchPath(item);

			foreach (var item in newDom.Python.Modules)
				dom.Python.ImportModule(item);
		}

		private void deferParse(StateModel sm, string fullName, XmlNode node)
		{
			var godel = new GodelContext
			{
				debugName = "{0} '{1}'".Fmt(node.ParentNode.Name, fullName),
				type = node.ParentNode.Name,
				Name = fullName,
				refName = node.getAttr("ref", null),
				inv = node.getAttr("inv", null),
				pre = node.getAttr("pre", null),
				post = node.getAttr("post", null)
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

		protected override Action handleAction(XmlNode node, State parent)
		{
			var action = base.handleAction(node, parent);

			if (action.type == "web")
			{
				if (!node.hasAttr("url"))
					throw new PeachException("Error, Action of type 'web' must have a 'url' attribute.");
				if (!node.hasAttr("method"))
					throw new PeachException("Error, Action of type 'web' must have a 'method' attribute.");

				var webAction = (WebAction) action;
				webAction.Url = node.getAttrString("url");
				webAction.Method = node.getAttrString("method");

				foreach (XmlNode child in node)
				{
					if (child.Name == "Part")
						handleActionPart(child, webAction);
					else if (child.Name == "Response")
						handleActionResponse(child, webAction);
					else if (child.Name == "Path")
						webAction.Paths.Add(ActionWebParameter.PitParser(this, child, webAction));
					else if (child.Name == "Query")
						webAction.Queries.Add(ActionWebParameter.PitParser(this, child, webAction));
					else if (child.Name == "Header")
						webAction.Headers.Add(ActionWebParameter.PitParser(this, child, webAction));
					else if (child.Name == "FormData")
						webAction.FormDatas.Add(ActionWebParameter.PitParser(this, child, webAction));
					else if (child.Name == "Body")
						webAction.Body = ActionWebParameter.PitParser(this, child, webAction);

				}
			}

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					var fullName = string.Join(".", action.parent.parent.Name, action.parent.Name, action.Name);
					deferParse((StateModel)parent.parent, fullName, child);
				}
			}

			var msg = action as Message;
			if (msg != null)
			{
				msg.Status = node.getAttrString("status");
				msg.Error = node.getAttrString("error");
			}

			return action;
		}

		protected void handleActionPart(XmlNode node, WebAction action)
		{
			var name = node.getAttrString("name");
			var param = new ActionWebPart(name)
			{
				action = action
			};

			foreach (XmlNode child in node)
			{
				switch (child.Name)
				{
					case "Header":
						var header = ActionWebParameter.PitParser(this, child, action);
						header.PartName = name;
						param.Headers.Add(header);
						break;
					case "FormData":
						var formData = ActionWebParameter.PitParser(this, child, action);
						formData.PartName = name;
						param.FormDatas.Add(formData);
						break;
					case "Body":
						var body = ActionWebParameter.PitParser(this, child, action);
						body.PartName = name;
						param.Body = body;
						break;
				}
			}

			action.Parts.Add(param);
		}
		protected void handleActionResponse(XmlNode node, WebAction action)
		{
			action.Response = new ActionWebResponse()
			{
				action = action
			};

			// Data model is not required.
			if (node.ChildNodes.Count > 0)
			{
				action.Response.dataModel = null;
				handleActionData(node, action.Response, "<Response> child of ", false);
			}
			else
				action.Response.dataModel = new DataModel("NULL");
		}


		protected override State handleState(XmlNode node, Peach.Core.Dom.StateModel parent)
		{
			var state = base.handleState(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
				{
					var fullName = string.Join(".", state.parent.Name, state.Name);
					deferParse((StateModel)parent, fullName, child);
				}
			}

			return state;
		}

		protected override Peach.Core.Dom.StateModel handleStateModel(XmlNode node, Peach.Core.Dom.Dom parent)
		{
			var stateModel = base.handleStateModel(node, parent);

			foreach (XmlNode child in node)
			{
				if (child.Name == "Godel")
					deferParse((StateModel)stateModel, stateModel.Name, child);
			}

			return stateModel;
		}
	}
}
