﻿
//
// Copyright (c) Michael Eddington
//
// Permission is hereby granted, free of charge, to any person obtaining a copy 
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights 
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
// copies of the Software, and to permit persons to whom the Software is 
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in	
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

// Authors:
//   Michael Eddington (mike@phed.org)

// $Id$

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Peach.Core;
using NLog;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Action types
	/// </summary>
	public enum ActionType
	{
		Unknown,

		Start,
		Stop,

		Accept,
		Connect,
		Open,
		Close,

		Input,
		Output,

		Call,
		SetProperty,
		GetProperty,

		ChangeState,
		Slurp
	}

	public delegate void ActionStartingEventHandler(Action action);
	public delegate void ActionFinishedEventHandler(Action action);

	/// <summary>
	/// Performs an Action such as sending output,
	/// calling a method, etc.
	/// </summary>
	public class Action : INamed
	{
		NLog.Logger logger = LogManager.GetLogger("Peach.Core.Dom.Action");
		public string _name = "Unknown Action";
		public ActionType type = ActionType.Unknown;

		public State parent = null;

		protected DataModel _dataModel;
		protected DataModel _origionalDataModel;
		protected Data _data;

		protected List<ActionParameter> _params = new List<ActionParameter>();

		protected string _publisher = null;
		protected string _when = null;
		protected string _onStart = null;
		protected string _onComplete = null;
		protected string _ref = null;
		protected string _method = null;
		protected string _property = null;
		protected string _value = null;
		protected string _setXpath = null;
		protected string _valueXpath = null;

		public string name
		{
			get { return _name; }
			set { _name = value; }
		}

		/// <summary>
		/// Data attached to action
		/// </summary>
		public Data data
		{
			get { return _data; }
			set { _data = value; }
		}

		/// <summary>
		/// Current copy of the data model we are mutating.
		/// </summary>
		public DataModel dataModel
		{
			get { return _dataModel; }
			set
			{
				if (_origionalDataModel == null)
				{
					_origionalDataModel = value;

					// Get the value to optimize next generation based on invalidation
					object tmp = _origionalDataModel.Value;
				}

				_dataModel = value;
			}
		}

		/// <summary>
		/// Origional copy of the data model we will be mutating.
		/// </summary>
		public DataModel origionalDataModel
		{
			get { return _origionalDataModel; }
			set { _origionalDataModel = value; }
		}

		public string value
		{
			get { return _value; }
			set { _value = value; }
		}

		public List<ActionParameter> parameters
		{
			get { return _params; }
			set { _params = value; }
		}

		/// <summary>
		/// xpath for selecting set targets during slurp.
		/// </summary>
		/// <remarks>
		/// Can return multiple elements.  All returned elements
		/// will be updated with a new value.
		/// </remarks>
		public string setXpath
		{
			get { return _setXpath; }
			set { _setXpath = value; }
		}

		/// <summary>
		/// xpath for selecting value during slurp
		/// </summary>
		/// <remarks>
		/// Must return a single element.
		/// </remarks>
		public string valueXpath
		{
			get { return _valueXpath; }
			set { _valueXpath = value; }
		}

		/// <summary>
		/// Name of publisher to use
		/// </summary>
		public string publisher
		{
			get { return _publisher; }
			set { _publisher = value; }
		}

		/// <summary>
		/// Only run action when expression is true
		/// </summary>
		public string when
		{
			get { return _when; }
			set { _when = value; }
		}

		/// <summary>
		/// Expression to run when action is starting
		/// </summary>
		public string onStart
		{
			get { return _onStart; }
			set { _onStart = value; }
		}

		/// <summary>
		/// Expression to run when action is completed
		/// </summary>
		public string onComplete
		{
			get { return _onComplete; }
			set { _onComplete = value; }
		}

		/// <summary>
		/// Name of state to change to, type=ChangeState
		/// </summary>
		public string reference
		{
			get { return _ref; }
			set { _ref = value; }
		}

		/// <summary>
		/// Method to call
		/// </summary>
		public string method
		{
			get { return _method; }
			set { _method = value; }
		}

		/// <summary>
		/// Property to operate on
		/// </summary>
		public string property
		{
			get { return _property; }
			set { _property = value; }
		}


		/// <summary>
		/// Action is starting to execute
		/// </summary>
		public static event ActionStartingEventHandler Starting;
		/// <summary>
		/// Action has finished executing
		/// </summary>
		public static event ActionFinishedEventHandler Finished;

		protected virtual void OnStarting()
		{
			if (Starting != null)
				Starting(this);
		}

		protected virtual void OnFinished()
		{
			if (Finished != null)
				Finished(this);
		}

		public void Run(RunContext context)
		{
			logger.Trace("Run({0}): {1}", name, type);

			try
			{
				Publisher publisher = null;
				if (this.publisher != null && this.publisher != "Peach.Agent")
				{
					if (!context.test.publishers.ContainsKey(this.publisher))
					{
						logger.Debug("Run: Publisher '" + this.publisher + "' not found!");
						throw new PeachException("Error, Action '"+name+"' publisher value '" + this.publisher + "' was not found!");
					}

					publisher = context.test.publishers[this.publisher];
				}
				else
				{
					publisher = context.test.publishers[0];
				}

				if (when != null)
				{
					Dictionary<string, object> state = new Dictionary<string, object>();
					state["action"] = this;
					state["state"] = this.parent;
					state["self"] = this;

					object value = Scripting.EvalExpression(when, state);
					if (!(value is bool))
					{
						logger.Debug("Run: when return is not boolean: " + value.ToString());
						return;
					}

					if (!(bool)value)
					{
						logger.Debug("Run: when returned false");
						return;
					}
				}

				OnStarting();

				switch (type)
				{
					case ActionType.Start:
						publisher.start(this);
						break;
					case ActionType.Stop:
						publisher.stop(this);
						break;
					case ActionType.Open:
					case ActionType.Connect:
						publisher.open(this);
						break;
					case ActionType.Close:
						publisher.close(this);
						break;

					case ActionType.Input:
						handleInput();
						break;
					case ActionType.Output:
						publisher.output(this, new Variant(dataModel.Value));
						break;

					case ActionType.Call:
						handleCall(publisher, context);
						break;
					case ActionType.GetProperty:
						handleGetProperty();
						break;
					case ActionType.SetProperty:
						handleSetProperty();
						break;

					case ActionType.ChangeState:
						handleChangeState();
						break;
					case ActionType.Slurp:
						handleSlurp();
						break;

					default:
						throw new ApplicationException("Error, Action.Run fell into unknown Action type handler!");
				}
			}
			finally
			{
				OnFinished();
			}
		}

		protected void handleInput()
		{
		}

		protected void handleCall(Publisher publisher, RunContext context)
		{
			// Are we sending to Agents?
			if (this.publisher == "Peach.Agent")
			{
				context.agentManager.Message("Action.Call", new Variant(this.method));

				Variant ret = new Variant(0);
				DateTime start = DateTime.Now;

				while (true)
				{
					ret = context.agentManager.Message("Action.Call.IsRunning", new Variant(this.method));
					if (ret != null && ((int)ret) == 0)
						break;

					// TODO - Expose 10 as the timeout
					if (DateTime.Now.Subtract(start).Seconds > 10)
						break;

					Thread.Sleep(200);
				}

				return;
			}

			publisher.call(this, method, parameters);
		}

		protected void handleGetProperty()
		{
		}

		protected void handleSetProperty()
		{
		}

		protected void handleChangeState()
		{
			if (!this.parent.parent.states.ContainsKey(reference))
			{
				logger.Debug("handleChangeState: Error, unable to locate state '" + reference + "'");
				throw new PeachException("Error, unable to locate state '" + reference + "' provided to action '" + name + "'");
			}

			logger.Debug("handleChangeState: Changing to state: " + reference);

			throw new ActionChangeStateException(this.parent.parent.states[reference]);
		}

		protected void handleSlurp()
		{
		}
	}

	public enum ActionParameterType
	{
		In,
		Out,
		InOut
	}

	public class ActionParameter
	{
		public ActionParameterType type;
		public DataElement dataModel;
		public object data;
	}

	public class ActionResult
	{
		DataElement dataModel;
	}

	public class ActionChangeStateException : Exception
	{
		public State changeToState;

		public ActionChangeStateException(State changeToState)
		{
			this.changeToState = changeToState;
		}
	}
}

// END
