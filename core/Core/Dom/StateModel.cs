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
//   Michael Eddington (mike@dejavusecurity.com)

// $Id$

using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Linq;
using Peach.Core.IO;

using NLog;
using System.ComponentModel;

namespace Peach.Core.Dom
{
	/// <summary>
	/// Defines a state machine to use during a fuzzing test.  State machines in Peach are intended to be
	/// fairly simple and allow for only the basic modeling typically required for fuzzing state aware protocols or 
	/// call sequences.  State machines are made up of one or more States which are in them selves make up of
	/// one or more Action.  As Actions are executed the data can be moved between them as needed.
	/// </summary>
	[Serializable]
	public class StateModel : INamed, IOwned<Dom>
	{
		#region Obsolete Functions

		[Obsolete("This property is obsolete and has been replaced by the Name property.")]
		public string name { get { return Name; } }

		#endregion

		[NonSerialized]
		private Dom _parent;

		static NLog.Logger logger = LogManager.GetCurrentClassLogger();

		public StateModel()
		{
			states = new NamedCollection<State>();
		}

		protected Dictionary<string, BitwiseStream> _dataActions = new Dictionary<string, BitwiseStream>();

		public IEnumerable<KeyValuePair<string, BitwiseStream>> dataActions
		{
			get
			{
				return _dataActions.AsEnumerable();
			}
		}

		/// <summary>
		/// Currently unused.  Exists for schema generation.
		/// </summary>
		[XmlElement("Godel")]
		[DefaultValue(null)]
		public Peach.Core.Xsd.Godel schemaGodel { get; set; }

		/// <summary>
		/// All states in state model.
		/// </summary>
		[XmlElement("State")]
		public NamedCollection<State> states { get; set; }

		/// <summary>
		/// The name of this state model.
		/// </summary>
		[XmlAttribute("name")]
		public string Name { get; set; }

		/// <summary>
		/// Name of the state to execute first.
		/// </summary>
		[XmlAttribute("initialState")]
		public string initialStateName { get; set; }

		/// <summary>
		/// Name of the state to execute last.
		/// </summary>
		[XmlAttribute("finalState")]
		[DefaultValue(null)]
		public string finalStateName { get; set; }

		/// <summary>
		/// The Dom that owns this state model.
		/// </summary>
		public Dom parent
		{
			get { return _parent; }
			set { _parent = value; }
		}

		/// <summary>
		/// The initial state to run when state machine executes.
		/// </summary>
		public State initialState { get; set; }

		/// <summary>
		/// The final state to run when state machine finishes.
		/// </summary>
		public State finalState { get; set; }

		/// <summary>
		/// Saves the data produced/consumed by an action for future logging.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		public void SaveData(string name, BitwiseStream value)
		{
			var key = "{0}.{1}.bin".Fmt(_dataActions.Count + 1, name);

			_dataActions.Add(key, value);
		}

		/// <summary>
		/// Start running the State Machine
		/// </summary>
		/// <remarks>
		/// This will start the initial State.
		/// </remarks>
		/// <param name="context"></param>
		public void Run(RunContext context)
		{
			var currentState = initialState;

			try
			{
				foreach (var publisher in context.test.publishers)
				{
					publisher.Iteration = context.test.strategy.Iteration;
					publisher.IsControlIteration = context.controlIteration;
				}

				_dataActions.Clear();

				// Update all data model to clones of origionalDataModel
				// before we start down the state path.
				foreach (State state in states)
					state.UpdateToOriginalDataModel();

				// If this is a record iteration, apply element mutability
				// after datasets and analyzers have been applied
				if (context.controlRecordingIteration)
					context.test.markMutableElements();

				context.OnStateModelStarting(this);

				// Allow mutating the initial state
				var newState = context.test.strategy.MutateChangingState(currentState);

				if (newState == currentState)
					logger.Debug("Run(): Changing to state \"{0}\".", newState.Name);
				else
					logger.Debug("Run(): Changing state mutated.  Switching to \"{0}\" instead of \"{1}\".",
						newState.Name, currentState);

				currentState = newState;

				// Main execution loop
				while (true)
				{
					try
					{
						currentState.Run(context);
						break;
					}
					catch (ActionChangeStateException ase)
					{
						if (ase.changeToState == finalState)
							throw new PeachException("Change state actions cannot refer to final state.");

						newState = context.test.strategy.MutateChangingState(ase.changeToState);

						if (newState == ase.changeToState)
							logger.Debug("Run(): Changing to state \"{0}\".", newState.Name);
						else
							logger.Debug("Run(): Changing state mutated.  Switching to \"{0}\" instead of \"{1}\".",
								newState.Name, ase.changeToState);

						context.OnStateChanging(currentState, newState);
						currentState = newState;
					}
				}
			}
			catch (ActionException)
			{
				// Exit state model!
			}
			finally
			{
				try
				{
					if (finalState != null)
					{
						logger.Debug("Run(): Executing final state \"{0}\".", finalState.Name);
						context.OnStateChanging(currentState, finalState);
						finalState.Run(context);
					}
				}
				catch (ActionChangeStateException)
				{
					throw new PeachException("A change state is not allowed in a final state.");
				}
				finally
				{
					foreach (var publisher in context.test.publishers)
						publisher.close();

					context.OnStateModelFinished(this);
				}
			}
		}

		[OnCloned]
		void OnCloned(StateModel original, object context)
		{
			foreach (var item in states)
				item.parent = this;
		}

		public bool HasFieldIds
		{
			get
			{
				foreach (var state in states)
				{
					if (!string.IsNullOrEmpty(state.FieldId))
						return true;

					foreach (var action in state.actions)
					{
						if (!string.IsNullOrEmpty(action.FieldId))
							return true;

						if (action.outputData.Any(
							actionData => actionData.dataModel
								.DisplayTraverse()
								.Any(e => !string.IsNullOrEmpty(e.FieldId))
							))
						{
							return true;
						}
					}
				}

				return false;
			}
		}
	}
}

// END
