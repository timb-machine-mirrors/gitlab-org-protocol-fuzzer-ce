
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
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading;

using Peach.Core.Agent;
using Peach.Core.Dom;

namespace Peach.Core
{
	/// <summary>
	/// Contains state information regarding the current fuzzing run.
	/// </summary>
	[Serializable]
	public class RunContext
	{
		#region Events

		#region Fault Collection

		public delegate void CollectFaultsHandler(RunContext context);

		/// <summary>
		/// This event is triggered after an interation has occured to allow
		/// collection of faults into RunContext.faults collection.
		/// </summary>
		public event CollectFaultsHandler CollectFaults;

		public void OnCollectFaults()
		{
			if (CollectFaults != null)
				CollectFaults(this);
		}

		#endregion

		#region Mutation Events

		public delegate void DataMutationEventHandler(RunContext context, ActionData actionData, DataElement element, Mutator mutator);

		public event DataMutationEventHandler DataMutating;

		public void OnDataMutating(ActionData actionData, DataElement element, Mutator mutator)
		{
			if (DataMutating != null)
				DataMutating(this, actionData, element, mutator);
		}

		public delegate void StateMutationEventHandler(RunContext context, State state, Mutator mutator);

		public event StateMutationEventHandler StateMutating;

		public void OnStateMutating(State state, Mutator mutator)
		{
			if (StateMutating != null)
				StateMutating(this, state, mutator);
		}

		#endregion

		#region State Model Events

		public delegate void StateModelEventHandler(RunContext context, StateModel stateModel);

		public event StateModelEventHandler StateModelStarting;

		public void OnStateModelStarting(StateModel stateModel)
		{
			if (StateModelStarting != null)
				StateModelStarting(this, stateModel);
		}

		public event StateModelEventHandler StateModelFinished;

		public void OnStateModelFinished(StateModel stateModel)
		{
			if (StateModelFinished != null)
				StateModelFinished(this, stateModel);
		}

		#endregion

		#region State Events

		public delegate void StateStartingEventHandler(RunContext context, State state);

		public event StateStartingEventHandler StateStarting;

		public void OnStateStarting(State state)
		{
			if (StateStarting != null)
				StateStarting(this, state);
		}

		public delegate void StateFinishedEventHandler(RunContext context, State state);

		public event StateFinishedEventHandler StateFinished;

		public void OnStateFinished(State state)
		{
			if (StateFinished != null)
				StateFinished(this, state);
		}

		public delegate void StateChangingEventHandler(RunContext context, State oldState, State newState);

		public event StateChangingEventHandler StateChanging;

		public void OnStateChanging(State oldState, State newState)
		{
			if (StateChanging != null)
				StateChanging(this, oldState, newState);
		}

		#endregion

		#region Action Events

		public delegate void ActionEventHandler(RunContext context, Dom.Action Action);

		public event ActionEventHandler ActionStarting;

		public void OnActionStarting(Dom.Action action)
		{
			if (ActionStarting != null)
				ActionStarting(this, action);
		}

		public event ActionEventHandler ActionFinished;

		public void OnActionFinished(Dom.Action action)
		{
			if (ActionFinished != null)
				ActionFinished(this, action);
		}

		#endregion

		#region Agent Events

		public delegate void AgentEventHandler(RunContext context, AgentClient agent);
		public delegate void MessageEventHandler(RunContext context, AgentClient agent, string name, Variant data);
		public delegate void CreatePublisherEventHandler(RunContext context, AgentClient agent, string cls, Dictionary<string, Variant> args);
		public delegate void StartMonitorEventHandler(RunContext context, AgentClient agent, string name, string cls, Dictionary<string, Variant> args);

		public event AgentEventHandler AgentConnect;

		public void OnAgentConnect(AgentClient agent)
		{
			if (AgentConnect != null)
				AgentConnect(this, agent);
		}

		public event AgentEventHandler AgentDisconnect;

		public void OnAgentDisconnect(AgentClient agent)
		{
			if (AgentDisconnect != null)
				AgentDisconnect(this, agent);
		}

		public event CreatePublisherEventHandler CreatePublisher;

		public void OnCreatePublisher(AgentClient agent, string cls, Dictionary<string, Variant> args)
		{
			if (CreatePublisher != null)
				CreatePublisher(this, agent, cls, args);
		}

		public event StartMonitorEventHandler StartMonitor;

		public void OnStartMonitor(AgentClient agent, string name, string cls, Dictionary<string, Variant> args)
		{
			if (StartMonitor != null)
				StartMonitor(this, agent, name, cls, args);
		}

		public event AgentEventHandler StopAllMonitors;

		public void OnStopAllMonitors(AgentClient agent)
		{
			if (StopAllMonitors != null)
				StopAllMonitors(this, agent);
		}

		public event AgentEventHandler SessionStarting;

		public void OnSessionStarting(AgentClient agent)
		{
			if (SessionStarting != null)
				SessionStarting(this, agent);
		}

		public event AgentEventHandler SessionFinished;

		public void OnSessionFinished(AgentClient agent)
		{
			if (SessionFinished != null)
				SessionFinished(this, agent);
		}

		public event AgentEventHandler IterationStarting;

		public void OnIterationStarting(AgentClient agent)
		{
			if (IterationStarting != null)
				IterationStarting(this, agent);
		}

		public event AgentEventHandler IterationFinished;

		public void OnIterationFinished(AgentClient agent)
		{
			if (IterationFinished != null)
				IterationFinished(this, agent);
		}

		public event AgentEventHandler DetectedFault;

		public void OnDetectedFault(AgentClient agent)
		{
			if (DetectedFault != null)
				DetectedFault(this, agent);
		}

		public event AgentEventHandler GetMonitorData;

		public void OnGetMonitorData(AgentClient agent)
		{
			if (GetMonitorData != null)
				GetMonitorData(this, agent);
		}

		public event AgentEventHandler MustStop;

		public void OnMustStop(AgentClient agent)
		{
			if (MustStop != null)
				MustStop(this, agent);
		}

		public event MessageEventHandler Message;

		public void OnMessage(AgentClient agent, string name, Variant data)
		{
			if (Message != null)
				Message(this, agent, name, data);
		}

		#endregion

		#endregion

		/// <summary>
		/// Configuration settings for this run
		/// </summary>
		public RunConfiguration config = null;

		/// <summary>
		/// Engine instance for this run
		/// </summary>
		[NonSerialized]
		public Engine engine = null;

		/// <summary>
		/// Dom to use for this run
		/// </summary>
		[NonSerialized]
		public Dom.Dom dom = null;

		/// <summary>
		/// Current test being run
		/// </summary>
		/// <remarks>
		/// Currently the Engine code sets this.
		/// </remarks>
		[NonSerialized]
		public Test test = null;

		/// <summary>
		/// Current agent manager for this run.
		/// </summary>
		/// <remarks>
		/// Currently the Engine code sets this.
		/// </remarks>
		[NonSerialized]
		public AgentManager agentManager = null;

		/// <summary>
		/// An object store that will last entire run.  For use
		/// by Peach code to store some state.
		/// </summary>
		[NonSerialized]
		public Dictionary<string, object> stateStore = new Dictionary<string, object>();

		/// <summary>
		/// An object store that will last current iteration.
		/// </summary>
		[NonSerialized]
		public Dictionary<string, object> iterationStateStore = new Dictionary<string, object>();

		/// <summary>
		/// The current iteration of fuzzing.
		/// </summary>
		public uint currentIteration = 0;

		#region Control Iterations

		/// <summary>
		/// Is this a control iteration.  Control iterations are used
		/// to verify the system can still reliably fuzz and are performed
		/// with out any mutations applied.
		/// </summary>
		/// <remarks>
		/// The first iteration is a special control iteration.  We also
		/// perform control iterations after we have collected a fault.
		/// 
		/// In later version we will likely inject control iterations every 
		/// N iterations where N is >= 100.
		/// </remarks>
		public bool controlIteration = false;

		/// <summary>
		/// Is this control operation also a recording iteration?
		/// </summary>
		/// <remarks>
		/// Recording iterations set our controlActionsExecuted and 
		/// controlStatesExecuted arrays.
		/// </remarks>
		public bool controlRecordingIteration = false;

		/// <summary>
		/// Actions performed during first control iteration.  Used to validate
		/// control iterations that come later have same action coverage.
		/// </summary>
		public List<Dom.Action> controlRecordingActionsExecuted = new List<Dom.Action>();

		/// <summary>
		/// States performed during first control iteration.  Used to validate
		/// control iterations that come later have same state coverage.
		/// </summary>
		/// <remarks>
		/// This may not be required with action coverage.
		/// </remarks>
		public List<Dom.State> controlRecordingStatesExecuted = new List<State>();

		/// <summary>
		/// Actions performed during later control iterations.  Used to validate
		/// control iterations that come later have same action coverage.
		/// </summary>
		public List<Dom.Action> controlActionsExecuted = new List<Dom.Action>();

		/// <summary>
		/// States performed during later control iterations.  Used to validate
		/// control iterations that come later have same state coverage.
		/// </summary>
		/// <remarks>
		/// This may not be required with action coverage.
		/// </remarks>
		public List<Dom.State> controlStatesExecuted = new List<State>();

		#endregion

		#region Faults

		/// <summary>
        /// Faults for current iteration of fuzzing.  This collection
        /// is cleared after each iteration.
        /// </summary>
        /// <remarks>
        /// This collection should only be added to from the CollectFaults event.
        /// </remarks>
        public List<Fault> faults = new List<Fault>();

		/// <summary>
		/// Controls if we continue fuzzing or exit
		/// after current iteration.  This can be used
		/// by UI code to stop Peach.
		/// </summary>
		private bool _continueFuzzing = true;

		public bool continueFuzzing 
		{
			get
			{
				if (!_continueFuzzing)
					return false;
				if (config != null && config.shouldStop != null)
					return !config.shouldStop();
				return true;
			}
			set
			{
				_continueFuzzing = value;
			}
		}

		#endregion

		#region Reproduce Fault

		/// <summary>
		/// True when we have found a fault and are in the process
		/// of reproducing it.
		/// </summary>
		/// <remarks>
		/// Many times, especially with network fuzzing, the iteration we detect a fault on is not the
		/// correct iteration, or the fault requires multiple iterations to reproduce.
		/// 
		/// Peach will start reproducing at the current iteration count then start moving backwards
		/// until we locate the iteration causing the crash, or reach our max back search value.
		/// </remarks>
		public bool reproducingFault = false;

		/// <summary>
		/// Number of iteration to search backwards trying to reproduce a fault.
		/// </summary>
		/// <remarks>
		/// Many times, especially with network fuzzing, the iteration we detect a fault on is not the
		/// correct iteration, or the fault requires multiple iterations to reproduce.
		/// 
		/// Peach will start reproducing at the current iteration count then start moving backwards
		/// until we locate the iteration causing the crash, or reach our max back search value.
		/// </remarks>
		public uint reproducingMaxBacksearch = 100;

		/// <summary>
		/// The initial iteration we detected fault on
		/// </summary>
		public uint reproducingInitialIteration = 0;

		/// <summary>
		/// This value times current iteration change is next iteration change.
		/// </summary>
		/// <remarks>
		/// Intial search process:
		/// 
		/// Move back 1
		/// Move back 1 * reproducingSkipMultiple = N
		/// Move back N * reproducingSkipMultiple = M
		/// Move back M * reproducingSkipMultiple = O
		/// Move back O * reproducingSkipMultiple ...
		/// 
		/// </remarks>
		public uint reproducingSkipMultiple = 2;

		/// <summary>
		/// Number of iterations to jump.
		/// </summary>
		/// <remarks>
		/// Initializes to 1, then multiply against reproducingSkipMultiple
		/// </remarks>
		public uint reproducingIterationJumpCount = 1;

		#endregion
	}
}

// end
