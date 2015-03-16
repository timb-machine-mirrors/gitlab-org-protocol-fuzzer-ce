using System;
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
			public abstract INamed Node { get; }

			protected int Index { get; set; }

			public virtual XPathNodeType NodeType
			{
				get { return XPathNodeType.Element; }
			}

			public virtual string Name
			{
				get { return Node.Name; }
			}

			public virtual string NamespaceUri
			{
				get
				{
					var idx = Node.Name.IndexOf(':');
					return idx < 0 ? string.Empty : Node.Name.Substring(0, idx);
				}
			}

			public virtual string LocalName
			{
				get
				{
					try
					{
						var idx = Node.Name.LastIndexOf(':');
						return idx < 0 ? Node.Name : Node.Name.Substring(idx + 1);
					}
					catch (ArgumentOutOfRangeException)
					{
						return "WTF";
					}
				}
			}

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
				return new NamedAttrEntry { Parent = Node };
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
			public INamed Parent { private get; set; }

			public override INamed Node
			{
				get { return Parent; }
			}

			public override XPathNodeType NodeType
			{
				get { return XPathNodeType.Attribute; }
			}

			public override string Name
			{
				get { return "name"; }
			}

			public override string NamespaceUri
			{
				get { return string.Empty; }
			}

			public override string LocalName
			{
				get { return Name; }
			}

			public override string Value
			{
				get { return Parent.Name; }
			}
		}

		#endregion

		#region Dom Entry

		class RootEntry : Entry
		{
			public Dom Dom { private get; set; }

			public override INamed Node
			{
				get { return Dom; }
			}

			public override XPathNodeType NodeType
			{
				get { return XPathNodeType.Root; }
			}

			public override Entry GetFirstChild()
			{
				if (Dom.tests.Count == 0)
					return null;

				return new TestEntry { Test = Dom.tests[0] };
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
			public Test Test { private get; set; }

			public override INamed Node { get { return Test; } }

			public override Entry GetFirstChild()
			{
				if (Test.stateModel == null)
					return null;

				return new StateModelEntry { StateModel = Test.stateModel };
			}

			public override Entry GetNext()
			{
				var tests = Test.parent.tests;
				var next = Index + 1;

				if (next == tests.Count)
					return null;

				return new TestEntry { Test = tests[next], Index = next };
			}

			public override Entry GetPrev()
			{
				var tests = Test.parent.tests;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new TestEntry { Test = tests[next], Index = next };
			}
		}

		#endregion

		#region State Model Entry

		class StateModelEntry : Entry
		{
			public StateModel StateModel { private get; set; }

			public override INamed Node { get { return StateModel; } }

			public override Entry GetFirstChild()
			{
				if (StateModel.states.Count == 0)
					return null;

				return new StateEntry { State = StateModel.states[0] };
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
			public State State { private get; set; }

			public override INamed Node { get { return State; } }

			public override Entry GetFirstChild()
			{
				if (State.actions.Count == 0)
					return null;

				return new ActionEntry { Action = State.actions[0] };
			}

			public override Entry GetNext()
			{
				var states = State.parent.states;
				var next = Index + 1;

				if (next == states.Count)
					return null;

				return new StateEntry { State = states[next], Index = next };
			}

			public override Entry GetPrev()
			{
				var states = State.parent.states;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new StateEntry { State = states[next], Index = next };
			}
		}

		#endregion

		#region Action Entry

		class ActionEntry : Entry
		{
			public Action Action { private get; set; }

			public override INamed Node { get { return Action; } }

			public override Entry GetFirstChild()
			{
				var actionData = Action.allData.FirstOrDefault();
				if (actionData == null)
					return null;

				return new ModelEntry { ActionData = actionData };
			}

			public override Entry GetNext()
			{
				var actions = Action.parent.actions;
				var next = Index + 1;

				if (next == actions.Count)
					return null;

				return new ActionEntry { Action = actions[next], Index = next };
			}

			public override Entry GetPrev()
			{
				var actions = Action.parent.actions;
				var next = Index - 1;

				if (next < 0)
					return null;

				return new ActionEntry { Action = actions[next], Index = next };
			}

			public override Entry GetFirstAttr()
			{
				return new ActionAttrEntry { Parent = Action };
			}
		}

		#endregion

		#region Action Attributes

		class ActionAttrEntry : Entry
		{
			public Action Parent { private get; set; }

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

			public override INamed Node
			{
				get { return Parent; }
			}

			public override XPathNodeType NodeType
			{
				get { return XPathNodeType.Attribute; }
			}

			public override string Name
			{
				get { return Attrs[Index].Item1; }
			}

			public override string NamespaceUri
			{
				get { return string.Empty; }
			}

			public override string LocalName
			{
				get { return Name; }
			}

			public override string Value
			{
				get { return Attrs[Index].Item2(Parent); }
			}

			public override Entry GetNextAttr()
			{
				var next = Index + 1;
				if (next == Attrs.Count)
					return null;

				return new ActionAttrEntry { Parent = Parent, Index = next };
			}
		}

		#endregion

		#region Data Model Entry

		class ModelEntry : Entry
		{
			public ActionData ActionData { private get; set; }

			public override INamed Node { get { return ActionData.dataModel; } }

			public override Entry GetFirstChild()
			{
				if (ActionData.dataModel.Count == 0)
					return null;

				return ElementEntry.Make(ActionData.dataModel);
			}

			public override Entry GetNext()
			{
				var idx = Index + 1;
				var next = ActionData.action.allData.ElementAtOrDefault(idx);

				if (next == null)
					return null;

				return new ModelEntry { ActionData = next, Index = idx };
			}

			public override Entry GetPrev()
			{
				if (Index == 0)
					return null;

				var idx = Index - 1;
				var next = ActionData.action.allData.ElementAtOrDefault(idx);

				if (next == null)
					return null;

				return new ModelEntry { ActionData = next, Index = idx };
			}

			public override Entry GetFirstAttr()
			{
				return new ElementAttrEntry { Parent = ActionData.dataModel };
			}
		}

		#endregion

		#region Data Element Entry

		class ElementEntry : Entry
		{
			static ElementEntry Make(Array elem)
			{
				return new ElementEntry { Peers = new[] { elem.OriginalElement }.Concat(elem).ToList() };
			}

			static ElementEntry Make(Choice elem)
			{
				if (elem.choiceElements.Count == 0)
					return null;

				return new ElementEntry { Peers = elem.choiceElements.Values.ToList() };
			}

			public static ElementEntry Make(DataElementContainer elem)
			{
				if (elem.Count == 0)
					return null;

				return new ElementEntry { Peers = elem };
			}

			static ElementEntry Make(DataElement elem)
			{
				var asArray = elem as Array;
				if (asArray != null)
					return Make(asArray);

				var asChoice = elem as Choice;
				if (asChoice != null)
					return Make(asChoice);

				var asCont = elem as DataElementContainer;
				if (asCont != null)
					return Make(asCont);

				return null;
			}

			private IList<DataElement> Peers { get; set; }

			public override INamed Node
			{
				get { return Peers[Index]; }
			}

			public override Entry GetFirstChild()
			{
				return Make(Peers[Index]);
			}

			public override Entry GetNext()
			{
				var next = Index + 1;

				if (next == Peers.Count)
					return null;

				return new ElementEntry { Peers = Peers, Index = next };
			}

			public override Entry GetPrev()
			{
				var next = Index - 1;

				if (next < 0)
					return null;

				return new ElementEntry { Peers = Peers, Index = next };
			}

			public override Entry GetFirstAttr()
			{
				return new ElementAttrEntry { Parent = Peers[Index] };
			}
		}

		#endregion

		#region Data Element Attributes

		class ElementAttrEntry : Entry
		{
			public DataElement Parent { private get; set; }

			static readonly List<Tuple<string, Func<DataElement, string>>> Attrs = new List<Tuple<string, Func<DataElement, string>>>
			(
				new[]
				{
					new Tuple<string, Func<DataElement, string>>("name", e => e.Name),
					new Tuple<string, Func<DataElement, string>>("isMutable", e => e.isMutable.ToString()),
					new Tuple<string, Func<DataElement, string>>("isToken", e => e.isToken.ToString())
				}
			);

			public override INamed Node
			{
				get { return Parent; }
			}

			public override XPathNodeType NodeType
			{
				get
				{
					return XPathNodeType.Attribute;
				}
			}

			public override string Name
			{
				get { return Attrs[Index].Item1; }
			}

			public override string NamespaceUri
			{
				get { return string.Empty; }
			}

			public override string LocalName
			{
				get { return Name; }
			}

			public override string Value
			{
				get { return Attrs[Index].Item2(Parent); }
			}

			public override Entry GetNextAttr()
			{
				var next = Index + 1;
				if (next == Attrs.Count)
					return null;

				return new ElementAttrEntry { Parent = Parent, Index = next };
			}
		}

		#endregion

		private LinkedList<Entry> _position;

		// ReSharper disable once InconsistentNaming
		[Obsolete("This property is obsolete. Use the 'CurrentNode' property instead.")]
		public object currentNode
		{
			get { return CurrentNode; }
		}

		public object CurrentNode
		{
			get { return _position.First.Value.Node; }
		}

		public PeachXPathNavigator(Dom dom)
			: this(new[] { new RootEntry { Dom = dom } })
		{
		}

		private PeachXPathNavigator(IEnumerable<Entry> position)
		{
			_position = new LinkedList<Entry>(position);
		}

		public override string BaseURI
		{
			get { return string.Empty; }
		}

		public override XPathNavigator Clone()
		{
			return new PeachXPathNavigator(_position);
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
			get { return _position.First.Value.LocalName; }
		}

		public override bool MoveTo(XPathNavigator other)
		{
			var asPeach = other as PeachXPathNavigator;
			if (asPeach == null)
				return false;

			_position = new LinkedList<Entry>(asPeach._position);
			return true;
		}

		public override bool MoveToFirstAttribute()
		{
			var attr = _position.First.Value.GetFirstAttr();
			if (attr == null)
				return false;

			_position.AddFirst(attr);
			return true;
		}

		public override bool MoveToFirstChild()
		{
			var child = _position.First.Value.GetFirstChild();
			if (child == null)
				return false;

			_position.AddFirst(child);
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
			var next = _position.First.Value.GetNext();
			if (next == null)
				return false;

			_position.RemoveFirst();
			_position.AddFirst(next);
			return true;
		}

		public override bool MoveToNextAttribute()
		{
			var next = _position.First.Value.GetNextAttr();
			if (next == null)
				return false;

			_position.RemoveFirst();
			_position.AddFirst(next);
			return true;
		}

		public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
		{
			return false;
		}

		public override bool MoveToParent()
		{
			if (_position.Count == 1)
				return false;

			_position.RemoveFirst();

			return true;
		}

		public override bool MoveToPrevious()
		{
			var next = _position.First.Value.GetPrev();
			if (next == null)
				return false;

			_position.RemoveFirst();
			_position.AddFirst(next);
			return true;
		}

		public override string Name
		{
			get { return _position.First.Value.Name; }
		}

		public override System.Xml.XmlNameTable NameTable
		{
			get { throw new NotImplementedException(); }
		}

		public override string NamespaceURI
		{
			get { return _position.First.Value.NamespaceUri; }
		}

		public override XPathNodeType NodeType
		{
			get { return _position.First.Value.NodeType; }
		}

		public override string Prefix
		{
			get { return string.Empty; }
		}

		public override string Value
		{
			get { return _position.First.Value.Value; }
		}
	}
}

#if DISABLED

	/// <summary>
	/// Create an XPath Navigator for Peach DOM objects.
	/// </summary>
	/// <remarks>
	/// The XPath query syntax is the purfect way to select nodes
	/// from a Peach DOM.  By implementing an XPathNavigator we 
	/// should beable to use the built in .NET XPath system with
	/// our Peach DOM.
	/// 
	/// This XPath navigator will only search root -> run -> test -> stateModel -> States* -> Actions* -> DataModels*.
	/// </remarks>
	public class PeachXPathNavigator : XPathNavigator
	{
		/// <summary>
		/// Attributes for each known type
		/// </summary>
		/// <remarks>
		/// List of property names that we will expose as "attributes"
		/// for the xpath expressions.
		/// </remarks>
		protected static Dictionary<Type, string[]> AttributeMatrix = new Dictionary<Type, string[]>();

		/// <summary>
		/// Map between Type and PeachXPathNodeType
		/// </summary>
		protected static Dictionary<Type, PeachXPathNodeType> NodeTypeMap = new Dictionary<Type, PeachXPathNodeType>();

		/// <summary>
		/// The Peach DOM we are navigating.
		/// </summary>
		public Dom dom;

		/// <summary>
		/// The current node/position in the dom.
		/// </summary>
		public object currentNode;

		/// <summary>
		/// Type of current node.
		/// </summary>
		public PeachXPathNodeType currentNodeType;

		/// <summary>
		/// Current attribute index.
		/// </summary>
		protected int attributeIndex = 0;

		/// <summary>
		/// Current test index.
		/// </summary>
		protected int testIndex = 0;

		/// <summary>
		/// Are we iterating attributes?
		/// </summary>
		protected bool iteratingAttributes = false;

		public object CurrentNode
		{
			get { return currentNode; }
		}

		static PeachXPathNavigator()
		{
			AttributeMatrix[typeof(Dom)] = new string[] { "name" };
			AttributeMatrix[typeof(DataElement)] = new string[] { "name", "isMutable", "isToken", "length" };
			AttributeMatrix[typeof(StateModel)] = new string[] { "name" };
			AttributeMatrix[typeof(State)] = new string[] { "name" };
			AttributeMatrix[typeof(Action)] = new string[] { "name", "type", "method", "property" };
			AttributeMatrix[typeof(Test)] = new string[] { "name" };

			NodeTypeMap[typeof(Dom)] = PeachXPathNodeType.Root;
			NodeTypeMap[typeof(DataElement)] = PeachXPathNodeType.DataModel;
			NodeTypeMap[typeof(StateModel)] = PeachXPathNodeType.StateModel;
			NodeTypeMap[typeof(State)] = PeachXPathNodeType.StateModel;
			NodeTypeMap[typeof(Action)] = PeachXPathNodeType.StateModel;
			NodeTypeMap[typeof(Test)] = PeachXPathNodeType.Test;
		}

		protected PeachXPathNodeType MapObjectToNodeType(object obj)
		{
			foreach(Type key in NodeTypeMap.Keys)
			{
				if (key.IsInstanceOfType(obj))
					return NodeTypeMap[key];
			}

			throw new ArgumentException("Object is of unknown type.");
		}

		public PeachXPathNavigator(Dom dom)
		{
			currentNode = dom;
			currentNodeType = PeachXPathNodeType.Root;
		}

		protected PeachXPathNavigator(Dom dom, object currentNode, PeachXPathNodeType currentNodeType, 
			int attributeIndex, bool iteratingAttributes)
		{
			this.dom = dom;
			this.currentNode = currentNode;
			this.currentNodeType = currentNodeType;
			this.attributeIndex = attributeIndex;
			this.iteratingAttributes = iteratingAttributes;
		}

		#region Abstract XPathNavigator

		public override string BaseURI
		{
			get { return string.Empty; }
		}

		public override XPathNavigator Clone()
		{
			return new PeachXPathNavigator(dom, currentNode, currentNodeType, attributeIndex, iteratingAttributes);
		}

		public override bool IsEmptyElement
		{
			get { return false; }
		}

		public override bool IsSamePosition(XPathNavigator other)
		{
			if (!(other is PeachXPathNavigator))
				return false;

			var otherXpath = other as PeachXPathNavigator;
			return (otherXpath.dom == dom && 
				otherXpath.currentNode == currentNode && 
				otherXpath.attributeIndex == attributeIndex);
		}

		public override string LocalName
		{
			get
			{
				if (iteratingAttributes)
					return GetCurrentNodeAttributeMatrix().ElementAt(attributeIndex);

				return ((INamed)currentNode).Name.Split(':').Last();
			}
		}

		public override bool MoveTo(XPathNavigator other)
		{
			//logger.Trace("MoveTo");

			var otherXpath = other as PeachXPathNavigator;
			if(otherXpath == null)
				return false;

			this.dom = otherXpath.dom;
			this.currentNode = otherXpath.currentNode;
			this.currentNodeType = otherXpath.currentNodeType;
			this.attributeIndex = otherXpath.attributeIndex;

			return true;
		}

		public override bool MoveToFirstAttribute()
		{
			//logger.Trace("MoveToFirstAttribute");

			iteratingAttributes = true;
			attributeIndex = 0;
			return true;
		}

		public override bool MoveToFirstChild()
		{
			//logger.Trace("MoveToFirstChild(" + ((INamed)currentNode).name + ")");

			if (currentNode is Choice)
			{
				var container = currentNode as Choice;
				if (container.choiceElements.Count == 0)
					return false;

				currentNode = container.choiceElements[0];
				return true;
			}

			var asArray = currentNode as Array;
			if (asArray != null)
			{
				if (asArray.OriginalElement != null)
				{
					currentNode = asArray.OriginalElement;
					return true;
				}

				if (asArray.Count > 0)
				{
					currentNode = asArray[0];
					return true;
				}

				return false;
			}

			if (currentNode is DataElementContainer)
			{
				var container = currentNode as DataElementContainer;
				if (container.Count == 0)
					return false;

				currentNode = container[0];
				return true;
			}
			else if (currentNode is DataElement)
			{
				return false;
			}
			else if (currentNode is Dom)
			{
				var dom = currentNode as Dom;

				if (dom.tests.Count > 0)
				{
					testIndex = 0;
					currentNode = dom.tests[0];
					currentNodeType = PeachXPathNodeType.Test;
					return true;
				}

				return false;
			}
			else if (currentNode is StateModel)
			{
				var stateModel = currentNode as StateModel;

				if (stateModel.states.Count == 0)
					return false;

				currentNode = stateModel.states[0];
				return true;
			}
			else if (currentNode is State)
			{
				var state = currentNode as State;
				if (state.actions.Count == 0)
					return false;

				currentNode = state.actions[0];
				return true;
			}
			else if (currentNode is Action)
			{
				var action = currentNode as Action;

				var data = action.allData.FirstOrDefault();
				if (data == null)
					return false;

				currentNode = data.dataModel;
				currentNodeType = PeachXPathNodeType.DataModel;
				return true;
			}
			else if (currentNode is Test)
			{
				var test = currentNode as Test;
				if (test.stateModel == null)
					return false;

				currentNode = test.stateModel;
				currentNodeType = PeachXPathNodeType.StateModel;
				return true;
			}

			throw new ArgumentException("Error, unknown type");
		}

		public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
		{
			//logger.Trace("MoveToFirstNamespace");

			return false;
		}

		public override bool MoveToId(string id)
		{
			//logger.Trace("MoveToId");

			return false;
		}

		public override bool MoveToNext()
		{
			//logger.Trace("MoveToNext(" + ((INamed)currentNode).name + ")");

			if (currentNodeType == PeachXPathNodeType.Root)
				return false;

			dynamic obj = currentNode;
			object parent = obj.parent;

			if (parent == null)
			{
				if (currentNode is DataModel)
				{
					if (obj.dom != null)
						parent = obj.dom;
					else if (obj.actionData != null)
						parent = obj.actionData.action;
				}

				if(parent == null)
					throw new Exception("Error, parent was unexpectedly null for object '" +
						obj.name + "' of type " + currentNode.GetType().ToString() + ".");
			}
			// DataModel drives from Block, so if our parent is a DataElementContainer we are all good
			if (currentNode is DataModel && !(parent is DataElementContainer))
			{
				var action = parent as Action;
				if (action == null)
					throw new Exception("Error, data model has weird parent!");

				// Find the first action data that is after the current data model
				var next = action.allData.SkipWhile(d => d.dataModel != currentNode).ElementAtOrDefault(1);

				if (next == null)
					return false;

				currentNode = next.dataModel;
				currentNodeType = PeachXPathNodeType.DataModel;
				return true;
			}

			if (currentNode is DataElement)
			{
				var asChoice = parent as Choice;

				if (asChoice != null)
				{
					var curr = (DataElement)currentNode;
					var next = asChoice.choiceElements
						.Select(kv => kv.Value)
						.SkipWhile(e => e != curr)
						.ElementAtOrDefault(1);

					if (next == null)
						return false;

					currentNode = next;
					return true;
				}

				var asArray = parent as Array;
				if (asArray != null && asArray.OriginalElement == currentNode && asArray.Count > 0)
				{
					currentNode = asArray[0];
					return true;
				}

				if (parent is DataElementContainer)
				{
					var curr = currentNode as DataElement;
					var block = parent as DataElementContainer;
					int index = block.IndexOf(curr);

					for (int i = index + 1; i < block.Count; ++i)
					{
						var elem = block[i];
						if (elem != curr)
						{
							currentNode = elem;
							return true;
						}
					}

					return false;
				}
				return false;
			}
			else if (currentNode is StateModel)
			{
				return false;
			}
			else if (currentNode is State)
			{
				var stateModel = parent as StateModel;
				int index = 0;
				for (int cnt = 0; cnt < stateModel.states.Count; cnt++)
				{
					if (stateModel.states[cnt] == currentNode)
					{
						index = cnt;
						break;
					}
				}

				if (stateModel.states.Count <= (index + 1))
					return false;

				currentNode = stateModel.states[index + 1];
				return true;
			}
			else if (currentNode is Action)
			{
				var state = parent as State;
				int index = state.actions.IndexOf((Action)currentNode);
				if (state.actions.Count <= (index + 1))
					return false;

				currentNode = state.actions[index + 1];
				return true;
			}
			else if (currentNode is Test)
			{
				var dom = parent as Dom;
				int index = dom.tests.IndexOf((Test)currentNode);
				if (dom.tests.Count <= (index + 1))
					return false;

				currentNode = dom.tests[index + 1];
				testIndex = index + 1;
				return true;
			}

			throw new ArgumentException("Error, unknown type");
		}

		public override bool MoveToNextAttribute()
		{
			//logger.Trace("MoveToNextAttribute");

			if (GetCurrentNodeAttributeMatrix().Length <= (attributeIndex + 1))
				return false;

			iteratingAttributes = true;
			attributeIndex++;
			return true;
		}

		protected string[] GetCurrentNodeAttributeMatrix()
		{
			foreach (Type key in AttributeMatrix.Keys)
			{
				if (key.IsInstanceOfType(currentNode))
					return AttributeMatrix[key];
			}

			return null;
		}

		public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
		{
			//logger.Trace("MoveToNextNamespace");

			return false;
		}

		public override bool MoveToParent()
		{
			//logger.Trace("MoveToParent({0}:{1})", currentNode.GetType(), ((INamed)currentNode).name);

			if (iteratingAttributes)
			{
				iteratingAttributes = false;
				return true;
			}

			if (currentNodeType == PeachXPathNodeType.Root)
				return false;

			dynamic obj = currentNode;

			// DataModel drives from Block, so if our parent is a DataElementContainer we are all good
			if (obj is DataModel && !(obj.parent is DataElementContainer))
			{
				if (obj.dom != null)
					currentNode = obj.dom;
				else if (obj.actionData != null)
					currentNode = obj.actionData.action;
				else
					throw new Exception("Error, data model with no dom/action parent!");
			}
			else if (obj is StateModel)
			{
				// state models have a parent of the dom, but we need to walk
				// back up to the test since that is how we descend
				Dom root = obj.parent as Dom;
				currentNode = root.tests[testIndex];
			}
			else
				currentNode = obj.parent;

			currentNodeType = MapObjectToNodeType(currentNode);

			return true;
		}

		public override bool MoveToPrevious()
		{
			//logger.Trace("MoveToPrevious");

			throw new NotImplementedException();
		}

		public override string Name
		{
			get { return ((INamed)currentNode).Name; }
		}

		public override System.Xml.XmlNameTable NameTable
		{
			get { throw new NotImplementedException(); }
		}

		public override string NamespaceURI
		{
			get
			{
				if (iteratingAttributes)
					return string.Empty;

				var parts = ((INamed)currentNode).Name.Split(':');
				return parts.Length > 1 ? parts[0] : string.Empty;
			}
		}

		public override XPathNodeType NodeType
		{
			get
			{
				if (iteratingAttributes)
					return XPathNodeType.Attribute;

				if (currentNodeType == PeachXPathNodeType.Root)
					return XPathNodeType.Root;

				return XPathNodeType.Element;
			}
		}

		public override string Prefix
		{
			get
			{
				return string.Empty;
			}
		}

		public override string Value
		{
			get
			{
				if (!iteratingAttributes)
					return string.Empty;

				string attr = LocalName;

				if (attr == "name")
					return ((INamed)currentNode).Name;

				if (currentNode is DataElement)
				{
					switch (attr)
					{
						case "isMutable": return ((DataElement)currentNode).isMutable.ToString();
						case "isToken": return ((DataElement)currentNode).isToken.ToString();
						case "length": return ((DataElement)currentNode).length.ToString();
					}
				}
				else if (currentNode is Action)
				{
					if (attr == "type")
						return ((Action)currentNode).GetType().Name;
					if (attr == "method" && currentNode is Actions.Call)
						return ((Actions.Call)currentNode).method;
					if (attr == "property" && currentNode is Actions.SetProperty)
						return ((Actions.SetProperty)currentNode).property;
					if (attr == "property" && currentNode is Actions.GetProperty)
						return ((Actions.GetProperty)currentNode).property;
				}

				return string.Empty;
			}
		}

		public override string GetAttribute(string localName, string namespaceURI)
		{
			return string.Empty;
		}

		#endregion

		#region XPathItem

		public override object TypedValue
		{
			get
			{
				return base.TypedValue;
			}
		}

		public override object ValueAs(Type returnType)
		{
			return base.ValueAs(returnType);
		}

		public override object ValueAs(Type returnType, System.Xml.IXmlNamespaceResolver nsResolver)
		{
			return base.ValueAs(returnType, nsResolver);
		}

		public override bool ValueAsBoolean
		{
			get
			{
				return base.ValueAsBoolean;
			}
		}

		public override DateTime ValueAsDateTime
		{
			get
			{
				return base.ValueAsDateTime;
			}
		}

		public override double ValueAsDouble
		{
			get
			{
				return base.ValueAsDouble;
			}
		}

		public override int ValueAsInt
		{
			get
			{
				return base.ValueAsInt;
			}
		}

		public override long ValueAsLong
		{
			get
			{
				return base.ValueAsLong;
			}
		}

		public override Type ValueType
		{
			get
			{
				return base.ValueType;
			}
		}

		public override System.Xml.Schema.XmlSchemaType XmlType
		{
			get
			{
				return base.XmlType;
			}
		}

		#endregion
	}
}
#endif
