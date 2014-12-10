
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
using System.Text;
using System.Threading;
using System.Linq;

using Peach.Core.Agent;
using Peach.Core.Dom;

using NLog;

namespace Peach.Core
{
	/// <summary>
	/// The main Peach fuzzing engine!
	/// </summary>
	public class Engine
	{
		static readonly NLog.Logger logger = LogManager.GetCurrentClassLogger();

		private readonly Watcher _watcher;
		private readonly RunContext _context;

		[Obsolete("This property is obsolete.")]
		public RunContext context { get { return _context; } }
		//public Dom.Dom dom { get { return runContext.dom; } }
		//public Test test  { get { return runContext.test; } }

		#region Events

		public delegate void TestStartingEventHandler(RunContext context);
		public delegate void IterationStartingEventHandler(RunContext context, uint currentIteration, uint? totalIterations);
		public delegate void IterationFinishedEventHandler(RunContext context, uint currentIteration);
		public delegate void FaultEventHandler(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData);
		public delegate void ReproFaultEventHandler(RunContext context, uint currentIteration, StateModel stateModel, Fault[] faultData);
		public delegate void ReproFailedEventHandler(RunContext context, uint currentIteration);
		public delegate void TestFinishedEventHandler(RunContext context);
		public delegate void TestWarningEventHandler(RunContext context, string msg);
		public delegate void TestErrorEventHandler(RunContext context, Exception e);
		public delegate void HaveCountEventHandler(RunContext context, uint totalIterations);
		public delegate void HaveParallelEventHandler(RunContext context, uint startIteration, uint stopIteration);

		/// <summary>
		/// Fired when a Test is starting.  This could be fired
		/// multiple times after the RunStarting event if the Run
		/// contains multiple Tests.
		/// </summary>
		public event TestStartingEventHandler TestStarting;

		/// <summary>
		/// Fired at the start of each iteration.  This event will
		/// be fired often.
		/// </summary>
		public event IterationStartingEventHandler IterationStarting;

		/// <summary>
		/// Fired at end of each iteration.  This event will be fired often.
		/// </summary>
		public event IterationFinishedEventHandler IterationFinished;

		/// <summary>
		/// Fired when a Fault is detected and the engine starts retrying to reproduce it.
		/// </summary>
		public event ReproFaultEventHandler ReproFault;

		/// <summary>
		/// Fired when a Fault is is unable to be reproduced
		/// </summary>
		public event ReproFailedEventHandler ReproFailed;

		/// <summary>
		/// Fired when a Fault is detected.
		/// </summary>
		public event FaultEventHandler Fault;

		/// <summary>
		/// Fired when a Test is finished.
		/// </summary>
		public event TestFinishedEventHandler TestFinished;

		/// <summary>
		/// Fired when a recoverable warning occurs during a Test.
		/// </summary>
		public event TestWarningEventHandler TestWarning;

		/// <summary>
		/// Fired when we know the count of iterations the Test will take.
		/// </summary>
		public event TestErrorEventHandler TestError;

		/// <summary>
		/// Fired when we know the count of iterations the Test will take.
		/// </summary>
		public event HaveCountEventHandler HaveCount;

		/// <summary>
		/// Fired when we know the range of iterations the parallel Test will take.
		/// </summary>
		public event HaveParallelEventHandler HaveParallel;

		private void OnTestStarting()
		{
			if (TestStarting != null)
				TestStarting(_context);
		}

		private void OnIterationStarting(uint currentIteration, uint? totalIterations)
		{
			if (IterationStarting != null)
				IterationStarting(_context, currentIteration, totalIterations);
		}

		private void OnIterationFinished(uint currentIteration)
		{
			if (IterationFinished != null)
				IterationFinished(_context, currentIteration);
		}

		private void OnFault(uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			logger.Debug(">> OnFault");

			if (Fault != null)
				Fault(_context, currentIteration, stateModel, faultData);

			logger.Debug("<< OnFault");
		}

		private void OnReproFault(uint currentIteration, StateModel stateModel, Fault[] faultData)
		{
			if (ReproFault != null)
				ReproFault(_context, currentIteration, stateModel, faultData);
		}

		private void OnReproFailed(uint currentIteration)
		{
			if (ReproFailed != null)
				ReproFailed(_context, currentIteration);
		}

		private void OnTestFinished()
		{
			if (TestFinished != null)
				TestFinished(_context);
		}

		private void OnTestError(Exception e)
		{
			if (TestError != null)
				TestError(_context, e);
		}

		private void OnTestWarning(string msg)
		{
			if (TestWarning != null)
				TestWarning(_context, msg);
		}

		private void OnHaveCount(uint totalIterations)
		{
			if (HaveCount != null)
				HaveCount(_context, totalIterations);
		}

		private void OnHaveParallel(uint startIteration, uint stopIteration)
		{
			if (HaveParallel != null)
				HaveParallel(_context, startIteration, stopIteration);
		}

		#endregion

		public Engine(Watcher watcher)
		{
			_watcher = watcher;
			_context = new RunContext
			{
				engine = this,
			};
		}

		/// <summary>
		/// Run the default fuzzing run in the specified dom.
		/// </summary>
		/// <param name="dom"></param>
		/// <param name="config"></param>
		public void startFuzzing(Dom.Dom dom, RunConfiguration config)
		{
			if (dom == null)
				throw new ArgumentNullException("dom");
			if (config == null)
				throw new ArgumentNullException("config");

			Test test;

			if (!dom.tests.TryGetValue(config.runName, out test))
				throw new PeachException("Unable to locate test named '" + config.runName + "'.");

			startFuzzing(dom, test, config);
		}

		protected void startFuzzing(Dom.Dom dom, Test test, RunConfiguration config)
		{
			if (dom == null)
				throw new ArgumentNullException("dom");
			if (test == null)
				throw new ArgumentNullException("test");
			if (config == null)
				throw new ArgumentNullException("config");

			_context.config = config;
			_context.dom = dom;
			_context.test = test;

			try
			{
				// Initialize any watchers and loggers
				if (_watcher != null)
					_watcher.Initialize(this, _context);

				foreach (var item in test.loggers)
					item.Initialize(this, _context);

				StartTest();

				RunTest();
			}
			finally
			{
				EndTest();

				foreach (var item in test.loggers)
					item.Finalize(this, _context);

				if (_watcher != null)
					_watcher.Finalize(this, _context);
			}
		}

		protected void StartTest()
		{
			_context.dom.context = _context;
		}

		protected void EndTest()
		{
			_context.dom.context = null;
		}

		/// <summary>
		/// Run a test case.  Contains main fuzzing loop.
		/// </summary>
		protected void RunTest()
		{
			var test = _context.test;
			var context = _context;

			try
			{
				context.test = test;
				context.agentManager = new AgentManager(context);
				context.reproducingFault = false;
				context.reproducingIterationJumpCount = 1;

				if (context.config.userDefinedSeed && !test.strategy.UsesRandomSeed)
				{
					var attr = test.strategy.GetType().GetDefaultAttr<MutationStrategyAttribute>();
					var name = attr != null ? attr.Name : test.strategy.GetType().Name;
					var msg = "The '{0}' mutation strategy does not allow setting the random seed.".Fmt(name);
					OnTestWarning(msg);
				}

				// Get mutation strategy
				MutationStrategy mutationStrategy = test.strategy;
				mutationStrategy.Initialize(context, this);

				uint iterationStart = 1;
				uint iterationStop = uint.MaxValue;
				uint? iterationTotal = null;
				uint lastControlIteration = 0;

				if (!mutationStrategy.IsDeterministic)
				{
					if (context.config.parallel)
						throw new PeachException("parallel is not supported when a non-deterministic mutation strategy is used");
					if (context.config.countOnly)
						throw new PeachException("count is not supported when a non-deterministic mutation strategy is used");
				}

				if (context.config.range)
				{
					if (context.config.parallel)
						throw new PeachException("range is not supported when parallel is used");

					logger.Debug("runTest: context.config.range == true, start: {0}, stop: {1}",
						context.config.rangeStart, context.config.rangeStop);

					iterationStart = context.config.rangeStart;
					iterationStop = context.config.rangeStop;
				}
				else if (context.config.skipToIteration > 1)
				{
					logger.Debug("runTest: context.config.skipToIteration == ",
						context.config.skipToIteration);

					iterationStart = context.config.skipToIteration;
				}

				iterationStart = Math.Max(1, iterationStart);

				uint lastReproFault = iterationStart - 1;
				uint iterationCount = iterationStart;
				bool firstRun = true;

				// First iteration is always a control/recording iteration
				context.controlIteration = true;
				context.controlRecordingIteration = true;

				// Initialize the current iteration prior to the TestStarting event
				context.currentIteration = iterationStart;

				test.markMutableElements();

				OnTestStarting();

				// Start agents
				foreach (var agent in test.agents)
				{
					// Only use agent if on correct platform
					if ((agent.platform & Platform.GetOS()) != Platform.OS.None)
					{
						try
						{
							// Note: We want to perfrom SessionStarting on each agent
							//       in turn.  We do this incase the first agent starts
							//       a virtual machine that contains the second agent.
							context.agentManager.AgentConnect(agent);
							context.agentManager.GetAgent(agent.name).SessionStarting();
						}
						catch (SoftException)
						{
							throw;
						}
						catch (PeachException)
						{
							throw;
						}
						catch (AgentException ae)
						{
							throw new PeachException("Agent Failure: " + ae.Message, ae);
						}
						catch (Exception ex)
						{
							throw new PeachException("General Agent Failure: " + ex.Message, ex);
						}
					}
				}

				while ((firstRun || iterationCount <= iterationStop) && context.continueFuzzing)
				{
					context.currentIteration = iterationCount;

					firstRun = false;

					// Clear out or iteration based state store
					context.iterationStateStore.Clear();

					// Should we perform a control iteration?
					if (test.controlIteration > 0 && !context.reproducingFault)
					{
						if ((test.controlIteration == 1 || iterationCount % test.controlIteration == 1) && lastControlIteration != iterationCount)
							context.controlIteration = true;
					}

					try
					{
						// Must set iteration 1st as strategy could enable control/record bools
						mutationStrategy.Iteration = iterationCount;

						if (context.controlIteration && context.controlRecordingIteration)
						{
							context.controlRecordingActionsExecuted.Clear();
							context.controlRecordingStatesExecuted.Clear();
						}

						context.controlActionsExecuted.Clear();
						context.controlStatesExecuted.Clear();


						if (context.config.singleIteration && !context.controlIteration)
						{
							logger.Debug("runTest: context.config.singleIteration == true");
							break;
						}

						// Make sure we are not hanging on to old faults.
						context.faults.Clear();

						try
						{
							OnIterationStarting(iterationCount, iterationTotal.HasValue ? iterationStop : iterationTotal);

							if (context.controlIteration)
							{
								if (context.controlRecordingIteration)
									logger.Debug("runTest: Performing recording iteration.");
								else
									logger.Debug("runTest: Performing control iteration.");
							}

							context.agentManager.IterationStarting(iterationCount, context.reproducingFault);

							test.stateModel.Run(context);
						}
						catch (SoftException se)
						{
							// We should just eat SoftExceptions.
							// They indicate we should move to the next
							// iteration.

							if (context.controlRecordingIteration)
							{
								logger.Debug("runTest: SoftException on recording iteration");
								if (se.InnerException != null && string.IsNullOrEmpty(se.Message))
									throw new PeachException(se.InnerException.Message, se);
								throw new PeachException(se.Message, se);
							}

							if (context.controlIteration)
							{
								logger.Debug("runTest: SoftException on control iteration, saving as fault");
								var ex = se.InnerException ?? se;
								OnControlFault("SoftException Detected:\n" + ex);
							}

							logger.Debug("runTest: SoftException, skipping to next iteration");
						}
						catch (OutOfMemoryException ex)
						{
							logger.Debug(ex.Message);
							logger.Debug(ex.StackTrace);
							logger.Debug("runTest: Warning: Iteration ended due to out of memory exception.  Continuing to next iteration.");

							throw new SoftException("Out of memory");
						}
						finally
						{
							context.agentManager.IterationFinished();

							OnIterationFinished(iterationCount);

							// If this was a control iteration, verify it againt our origional
							// recording.
							if (context.faults.Count == 0 &&
								context.controlRecordingIteration == false &&
								context.controlIteration &&
								!test.nonDeterministicActions)
							{
								if (context.controlRecordingActionsExecuted.Count != context.controlActionsExecuted.Count)
								{
									string description = string.Format(@"The Peach control iteration performed failed
to execute same as initial control.  Number of actions is different. {0} != {1}",
										context.controlRecordingActionsExecuted.Count,
										context.controlActionsExecuted.Count);

									logger.Debug(description);
									OnControlFault(description);
								}
								else if (context.controlRecordingStatesExecuted.Count != context.controlStatesExecuted.Count)
								{
									string description = string.Format(@"The Peach control iteration performed failed
to execute same as initial control.  Number of states is different. {0} != {1}",
										context.controlRecordingStatesExecuted.Count,
										context.controlStatesExecuted.Count);

									logger.Debug(description);
									OnControlFault(description);
								}

								// Check states first, since actions will always be different if
								// states are not executed
								if (context.faults.Count == 0)
								{
									var missedStates = context.controlRecordingStatesExecuted
										.Where(s => !context.controlStatesExecuted.Contains(s))
										.ToList();

									if (missedStates.Count > 0)
									{
										var sb = new StringBuilder();
										sb.Append("The Peach control iteration performed failed to execute same as initial control. ");

										if (missedStates.Count == 1)
										{
											sb.AppendFormat("State '{0}' was not performed.", missedStates[0].name);
										}
										else
										{
											sb.AppendLine("The following states were not performed:");
											foreach (var s in missedStates)
												sb.AppendLine("\t'{0}'".Fmt(s.name));
										}

										var description = sb.ToString();

										logger.Debug(description);
										OnControlFault(description);
									}
								}

								if (context.faults.Count == 0)
								{
									var missedActions = context.controlRecordingActionsExecuted
										.Where(a => !context.controlActionsExecuted.Contains(a))
										.ToList();

									if (missedActions.Count > 0)
									{
										var sb = new StringBuilder();
										sb.Append("The Peach control iteration performed failed to execute same as initial control. ");

										if (missedActions.Count == 1)
										{
											sb.AppendFormat("Action '{0}.{1}' was not performed.", missedActions[0].parent.name, missedActions[0].name);
										}
										else
										{
											sb.AppendLine("The following actions were not performed:");
											foreach (var a in missedActions)
												sb.AppendLine("\t'{0}.{1}'".Fmt(a.parent.name, a.name));
										}

										var description = sb.ToString();

										logger.Debug(description);
										OnControlFault(description);
									}
								}
							}
						}

						// User can specify a time to wait between iterations
						// we can use that time to better detect faults
						if (context.test.waitTime > 0)
							Thread.Sleep(TimeSpan.FromSeconds(context.test.waitTime));

						if (context.reproducingFault)
						{
							// User can specify a time to wait between iterations
							// when reproducing faults.
							if (context.test.faultWaitTime > 0)
								Thread.Sleep(TimeSpan.FromSeconds(context.test.faultWaitTime));
						}

						// Collect any faults that were found
						context.OnCollectFaults();

						if (context.faults.Count > 0)
						{
							logger.Debug("runTest: detected fault on iteration {0}", iterationCount);

							foreach (Fault fault in context.faults)
							{
								fault.iteration = iterationCount;
								fault.controlIteration = context.controlIteration;
								fault.controlRecordingIteration = context.controlRecordingIteration;
							}

							if (context.reproducingFault)
								OnFault(iterationCount, test.stateModel, context.faults.ToArray());
							else
								OnReproFault(iterationCount, test.stateModel, context.faults.ToArray());

							if (context.controlRecordingIteration && context.reproducingFault)
							{
								logger.Debug("runTest: Fault detected on control iteration");
								throw new PeachException("Fault detected on control iteration.");
							}

							if (context.controlIteration && context.reproducingFault && test.TargetLifetime == Test.Lifetime.Iteration)
							{
								logger.Debug("runTest: Fault detected on control iteration");
								throw new PeachException("Fault detected on control iteration.");
							}

							if (context.reproducingFault)
							{
								lastReproFault = iterationCount;

								// If we have moved less than 20 iterations, start fuzzing
								// from here thinking we may have not really performed the
								// next few iterations.

								// Otherwise skip forward to were we left off.

								if (context.reproducingInitialIteration - iterationCount > 20)
								{
									iterationCount = context.reproducingInitialIteration;
								}

								context.reproducingFault = false;
								context.reproducingIterationJumpCount = 1;

								logger.Debug("runTest: Reproduced fault, continuing fuzzing at iteration {0}", iterationCount);
							}
							else
							{
								logger.Debug("runTest: Attempting to reproduce fault.");

								context.reproducingFault = true;
								context.reproducingInitialIteration = iterationCount;
								context.reproducingIterationJumpCount = 1;

								logger.Debug("runTest: replaying iteration " + iterationCount);
							}
						}
						else if (context.reproducingFault)
						{
							if (test.TargetLifetime == Test.Lifetime.Iteration)
							{
								logger.Debug("runTest: Could not reproducing fault.");

								context.reproducingFault = false;
								iterationCount = context.reproducingInitialIteration;

								OnReproFailed(iterationCount);
							}
							else if (test.TargetLifetime == Test.Lifetime.Session)
							{
								uint maxJump = context.reproducingInitialIteration - lastReproFault - 1;

								if (context.reproducingIterationJumpCount >= (maxJump*2) ||
								    context.reproducingIterationJumpCount > test.MaxBackSearch)
								{
									logger.Debug("runTest: Giving up reproducing fault, reached max backsearch.");

									context.reproducingFault = false;
									iterationCount = context.reproducingInitialIteration;

									OnReproFailed(iterationCount);
								}
								else
								{
									uint delta = Math.Min(maxJump, context.reproducingIterationJumpCount);
									iterationCount = context.reproducingInitialIteration - delta - 1;

									logger.Debug("runTest: Moving backwards {0} iterations to reproduce fault.", delta);
								}

								// Make next jump larger
								context.reproducingIterationJumpCount *= context.reproducingSkipMultiple;
							}
						}

						if (context.agentManager.MustStop())
						{
							logger.Debug("runTest: agents say we must stop!");

							throw new PeachException("Error, agent monitor stopped run!");
						}

						if (context.faults.Count > 0 && context.reproducingFault)
							continue;

						// Update our totals and stop based on new count
						if (context.controlIteration && context.controlRecordingIteration && !iterationTotal.HasValue)
						{
							if (context.config.countOnly)
							{
								OnHaveCount(mutationStrategy.Count);
								break;
							}

							iterationTotal = mutationStrategy.Count;
							if (iterationTotal < iterationStop)
								iterationStop = iterationTotal.Value;

							if (context.config.parallel)
							{
								if (iterationTotal < context.config.parallelTotal)
									throw new PeachException(string.Format("Error, {1} parallel machines is greater than the {0} total iterations.", iterationTotal, context.config.parallelTotal));

								var range = Utilities.SliceRange(1, iterationStop, context.config.parallelNum, context.config.parallelTotal);

								iterationStart = range.Item1;
								iterationStop = range.Item2;

								OnHaveParallel(iterationStart, iterationStop);

								if (context.config.skipToIteration > iterationStart)
									iterationStart = context.config.skipToIteration;

								iterationCount = iterationStart;
							}
						}

						// Don't increment the iteration count if we are on a 
						// control iteration
						if (!context.controlIteration)
							++iterationCount;
					}
					finally
					{
						if (!context.reproducingFault)
						{
							if (context.controlIteration)
								lastControlIteration = iterationCount;

							context.controlIteration = false;
							context.controlRecordingIteration = false;
						}
					}
				}
			}
			catch (Exception e)
			{
				OnTestError(e);
				throw;
			}
			finally
			{
				foreach (Publisher publisher in context.test.publishers.Values)
				{
					try
					{
						publisher.stop();
					}
					catch
					{
					}
				}

				context.agentManager.SessionFinished();
				context.agentManager.StopAllMonitors();
				context.agentManager.Shutdown();
				OnTestFinished();

				context.test = null;

				test.strategy.Finalize(context, this);
			}
		}

		private void OnControlFault(string description)
		{
			// Don't tell the engine to stop, let the replay logic determine what to do
			// If a fault is detected or reproduced on a control iteration the engine
			// will automatically stop.

			var fault = new Fault
			{
				detectionSource = "PeachControlIteration",
				iteration = _context.currentIteration,
				controlIteration = _context.controlIteration,
				controlRecordingIteration = _context.controlRecordingIteration,
				title = "Peach Control Iteration Failed",
				description = description,
				folderName = "ControlIteration",
				type = FaultType.Fault
			};

			_context.faults.Add(fault);
		}
	}
}

// end
