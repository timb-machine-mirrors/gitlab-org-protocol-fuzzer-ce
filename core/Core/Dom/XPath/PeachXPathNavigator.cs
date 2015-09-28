﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.XPath;

namespace Peach.Core.Dom.XPath
{
	public class PeachXPathNavigator : XPathNavigator
	{
		#region Base Node Entry

		abstract class Entry : IEquatable<Entry>
		{
			protected Entry(INamed node, int index)
			{
				Index = index;
				Node = node;
				Name = node.Name;
				NamespaceUri = string.Empty;
				LocalName = Name;
				NodeType = XPathNodeType.Element;
			}

			protected Entry(INamed node, int index, string name, XPathNodeType nodeType)
			{
				Index = index;
				Node = node;
				Name = name;
				NamespaceUri = string.Empty;
				LocalName = Name;
				NodeType = nodeType;
			}

			public INamed Node { get; private set;}

			public string Name { get; private set; }

			protected int Index { get; private set; }

			public XPathNodeType NodeType { get; private set; }

			public string NamespaceUri { get; protected set; }

			public string LocalName { get; protected set; }

			public virtual string Value
			{
				get { return string.Empty; }
			}

			public virtual Entry GetFirstChild()
			{
				return null;
			}

			public virtual Entry GetNext()
			{
				return null;
			}

			public virtual Entry GetPrev()
			{
				return null;
			}

			public virtual Entry GetFirstAttr()
			{
				return new NamedAttrEntry(Node);
			}

			public virtual Entry GetNextAttr()
			{
				return null;
			}

			public bool Equals(Entry rhs)
			{
				if (rhs == null)
					return false;

				return Node == rhs.Node && Index == rhs.Index && NodeType == rhs.NodeType;
			}
		}

		#endregion

		#region Name Attribute Entry

		class NamedAttrEntry : Entry
		{
			public NamedAttrEntry(INamed node)
				: base(node, 0, "name", XPathNodeType.Attribute)
			{
			}

			public override string Value
			{
				get { return Node.Name; }
			}
		}

		#endregion

		#region Dom Entry

		class RootEntry : Entry
		{
			private readonly Dom _dom;

			public RootEntry(Dom dom)
				: base(dom, 0, dom.Name, XPathNodeType.Root)
			{
				_dom = dom;
			}

			public override Entry GetFirstChild()
			{
				if (_dom.tests.Count == 0)
					return null;

				return new TestEntry(_dom.tests[0], 0);
			}

			public override Entry GetFirstAttr()
			{
				return null;
			}
		}

		#endregion

		#region Test Entry

		class TestEntry : Entry
		{
			private readonly Test _test;

			public TestEntry(Test test, int index)
				: base(test, index)
			{
				_test = test;
			}

			public override Entry GetFirstChild()
			{
				if (_test.stateModel == null)
					return null;

				return new StateModelEntry(_test.stateModel);
			}

			public override Entry GetNext()
			{
				var tests = _test.parent.tests;
				var next = Index + 1;

				if (next == tests.Count)
					return null;

				return new TestEntry(tests[next], next);
			}

			public override Entry GetPrev()
			{
				var tests = _test.parent.tests;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new TestEntry(tests[next], next);
			}
		}

		#endregion

		#region State Model Entry

		class StateModelEntry : Entry
		{
			private readonly StateModel _stateModel;

			public StateModelEntry(StateModel stateModel)
				: base(stateModel, 0)
			{
				_stateModel = stateModel;

				var idx = Name.LastIndexOf(':');
				if (idx > 0)
				{
					NamespaceUri = Name.Substring(0, idx);
					LocalName = Name.Substring(idx + 1);
				}
			}

			public override Entry GetFirstChild()
			{
				if (_stateModel.states.Count == 0)
					return null;

				return new StateEntry(_stateModel.states[0], 0);
			}

			public override Entry GetNext()
			{
				return null;
			}

			public override Entry GetPrev()
			{
				return null;
			}
		}

		#endregion

		#region State Entry

		class StateEntry : Entry
		{
			private readonly State _state;

			public StateEntry(State state, int index)
				: base(state, index)
			{
				_state = state;
			}

			public override Entry GetFirstChild()
			{
				if (_state.actions.Count == 0)
					return null;

				return new ActionEntry(_state.actions[0], 0);
			}

			public override Entry GetNext()
			{
				var states = _state.parent.states;
				var next = Index + 1;

				if (next == states.Count)
					return null;

				return new StateEntry(states[next], next);
			}

			public override Entry GetPrev()
			{
				var states = _state.parent.states;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new StateEntry(states[next], next);
			}
		}

		#endregion

		#region Action Entry

		class ActionEntry : Entry
		{
			private readonly Action _action;

			public ActionEntry(Action action, int index)
				: base(action, index)
			{
				_action = action;
			}

			public override Entry GetFirstChild()
			{
				var actionData = _action.allData.FirstOrDefault();
				if (actionData == null)
					return null;

				return new ModelEntry(actionData, 0);
			}

			public override Entry GetNext()
			{
				var actions = _action.parent.actions;
				var next = Index + 1;

				if (next == actions.Count)
					return null;

				return new ActionEntry(actions[next], next);
			}

			public override Entry GetPrev()
			{
				var actions = _action.parent.actions;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new ActionEntry(actions[next], next);
			}

			public override Entry GetFirstAttr()
			{
				return new ActionAttrEntry(_action, 0);
			}
		}

		#endregion

		#region Action Attributes

		class ActionAttrEntry : Entry
		{
			private readonly Action _action;

			public ActionAttrEntry(Action action, int index)
				: base(action, index, Attrs[index].Item1, XPathNodeType.Attribute)
			{
				_action = action;
			}

			static readonly List<Tuple<string, Func<Action, string>>> Attrs = new List<Tuple<string, Func<Action, string>>>
			(
				new[]
				{
					new Tuple<string, Func<Action, string>>("name", a => a.Name),
					new Tuple<string, Func<Action, string>>("type", a => a.type),
					new Tuple<string, Func<Action, string>>("method", GetMethod),
					new Tuple<string, Func<Action, string>>("property", GetProperty)
				}
			);

			private static string GetMethod(Action a)
			{
				var asCall = a as Actions.Call;
				return asCall == null ? string.Empty : asCall.method;
			}

			private static string GetProperty(Action a)
			{
				var asSet = a as Actions.SetProperty;
				if (asSet != null)
					return asSet.property;

				var asGet = a as Actions.GetProperty;
				return asGet == null ? string.Empty : asGet.property;
			}

			public override string Value
			{
				get { return Attrs[Index].Item2(_action); }
			}

			public override Entry GetNextAttr()
			{
				var next = Index + 1;
				if (next == Attrs.Count)
					return null;

				return new ActionAttrEntry(_action, next);
			}
		}

		#endregion

		#region Data Model Entry

		class ModelEntry : Entry
		{
			private readonly ActionData _actionData;

			public ModelEntry(ActionData actionData, int index)
				: base(actionData.dataModel, index)
			{
				_actionData = actionData;

				var idx = Name.LastIndexOf(':');
				if (idx > 0)
				{
					NamespaceUri = Name.Substring(0, idx);
					LocalName = Name.Substring(idx + 1);
				}
			}

			public override Entry GetFirstChild()
			{
				if (_actionData.dataModel.Count == 0)
					return null;

				return ElementEntry.Make(_actionData.dataModel);
			}

			public override Entry GetNext()
			{
				var idx = Index + 1;
				var next = _actionData.action.allData.ElementAtOrDefault(idx);

				if (next == null)
					return null;

				return new ModelEntry(next, idx);
			}

			public override Entry GetPrev()
			{
				if (Index == 0)
					return null;

				var idx = Index - 1;
				var next = _actionData.action.allData.ElementAtOrDefault(idx);

				if (next == null)
					return null;

				return new ModelEntry(next, idx);
			}

			public override Entry GetFirstAttr()
			{
				return new ElementAttrEntry(_actionData.dataModel, 0);
			}
		}

		#endregion

		#region Data Element Entry

		class ElementEntry : Entry
		{
			public static ElementEntry Make(DataElement parent)
			{
				var peers = parent.XPathChildren();
				if (peers.Count == 0)
					return null;

				return new ElementEntry(peers, 0);
			}

			private readonly IList<DataElement> _peers;

			private ElementEntry(IList<DataElement> peers, int index)
				: base(peers[index], index)
			{
				_peers = peers;
			}

			public override Entry GetFirstChild()
			{
				return Make(_peers[Index]);
			}

			public override Entry GetNext()
			{
				var next = Index + 1;

				if (next == _peers.Count)
					return null;

				return new ElementEntry(_peers, next);
			}

			public override Entry GetPrev()
			{
				var next = Index - 1;

				if (next < 0)
					return null;

				return new ElementEntry(_peers, next);
			}

			public override Entry GetFirstAttr()
			{
				return new ElementAttrEntry(_peers[Index], 0);
			}
		}

		#endregion

		#region Data Element Attributes

		class ElementAttrEntry : Entry
		{
			private readonly DataElement _element;

			public ElementAttrEntry(DataElement element, int index)
				: base(element, index, Attrs[index].Item1, XPathNodeType.Attribute)
			{
				_element = element;
			}

			static readonly List<Tuple<string, Func<DataElement, string>>> Attrs = new List<Tuple<string, Func<DataElement, string>>>
			(
				new[]
				{
					new Tuple<string, Func<DataElement, string>>("name", e => e.Name),
					new Tuple<string, Func<DataElement, string>>("isMutable", e => e.isMutable.ToString()),
					new Tuple<string, Func<DataElement, string>>("isToken", e => e.isToken.ToString())
				}
			);

			public override string Value
			{
				get { return Attrs[Index].Item2(_element); }
			}

			public override Entry GetNextAttr()
			{
				var next = Index + 1;
				if (next == Attrs.Count)
					return null;

				return new ElementAttrEntry(_element, next);
			}
		}

		#endregion

		// Stack of previous nodes so MoveToParent is quick
		// Use a LinkedList so Clone() doesn't reverse our history!
		private LinkedList<Entry> _position;

		// Current node is not contained in the position list
		// so that MoveNext/MovePrev doesn't have to do a Pop()/Push()
		private Entry _currentNode;

		// ReSharper disable once InconsistentNaming
		[Obsolete("This property is obsolete. Use the 'CurrentNode' property instead.")]
		public object currentNode
		{
			get { return CurrentNode; }
		}

		public object CurrentNode
		{
			get { return _currentNode.Node; }
		}

		[Obsolete("This constructor is obsolete. Construct with a state model instead.")]
		public PeachXPathNavigator(Dom dom)
			: this(dom.context.test.stateModel)
		{
		}

		public PeachXPathNavigator(StateModel sm)
		{
			_position = new LinkedList<Entry>();
			_currentNode = new StateModelEntry(sm);
		}

		private PeachXPathNavigator(PeachXPathNavigator other)
		{
			_position = new LinkedList<Entry>(other._position);
			_currentNode = other._currentNode;
		}

		public override string BaseURI
		{
			get { return string.Empty; }
		}

		public override XPathNavigator Clone()
		{
			return new PeachXPathNavigator(this);
		}

		public override bool IsEmptyElement
		{
			get { return false; }
		}

		public override bool IsSamePosition(XPathNavigator other)
		{
			var asPeach = other as PeachXPathNavigator;
			if (asPeach == null)
				return false;

			if (!_currentNode.Equals(asPeach._currentNode))
				return false;

			var lhs = _position.GetEnumerator();
			var rhs = asPeach._position.GetEnumerator();

			while (true)
			{
				var hasLhs = lhs.MoveNext();
				var hasRhs = rhs.MoveNext();

				if (hasLhs != hasRhs)
					return false;

				if (!hasLhs)
					return true;

				Debug.Assert(lhs.Current != null);

				if (!lhs.Current.Equals(rhs.Current))
					return false;
			}
		}

		public override string LocalName
		{
			get { return _currentNode.LocalName; }
		}

		public override bool MoveTo(XPathNavigator other)
		{
			var asPeach = other as PeachXPathNavigator;
			if (asPeach == null)
				return false;

			_position = new LinkedList<Entry>(asPeach._position);
			_currentNode = asPeach._currentNode;

			return true;
		}

		public override bool MoveToFirstAttribute()
		{
			var attr = _currentNode.GetFirstAttr();
			if (attr == null)
				return false;

			_position.AddFirst(_currentNode);
			_currentNode = attr;

			return true;
		}

		public override bool MoveToFirstChild()
		{
			var child = _currentNode.GetFirstChild();
			if (child == null)
				return false;

			_position.AddFirst(_currentNode);
			_currentNode = child;

			return true;
		}

		public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
		{
			return false;
		}

		public override bool MoveToId(string id)
		{
			return false;
		}

		public override bool MoveToNext()
		{
			var next = _currentNode.GetNext();
			if (next == null)
				return false;

			_currentNode = next;

			return true;
		}

		public override bool MoveToNextAttribute()
		{
			var next = _currentNode.GetNextAttr();
			if (next == null)
				return false;

			_currentNode = next;

			return true;
		}

		public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
		{
			return false;
		}

		public override bool MoveToParent()
		{
			if (_position.Count == 0)
				return false;

			_currentNode = _position.First.Value;
			_position.RemoveFirst();

			return true;
		}

		public override bool MoveToPrevious()
		{
			var prev = _currentNode.GetPrev();
			if (prev == null)
				return false;

			_currentNode = prev;

			return true;
		}

		public override string Name
		{
			get { return _currentNode.Name; }
		}

		public override System.Xml.XmlNameTable NameTable
		{
			get { throw new NotImplementedException(); }
		}

		public override string NamespaceURI
		{
			get { return _currentNode.NamespaceUri; }
		}

		public override XPathNodeType NodeType
		{
			get { return _currentNode.NodeType; }
		}

		public override string Prefix
		{
			get { return string.Empty; }
		}

		public override string Value
		{
			get { return _currentNode.Value; }
		}
	}
}
