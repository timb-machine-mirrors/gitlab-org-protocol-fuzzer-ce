using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common;
using System.IO;
using PeachFarm.Common.Messages;

namespace PeachFarm.Admin.Console
{
	class Program
	{
		private static Admin admin = null;

		static void syntax() { throw new SyntaxException(); }

		static void Main(string[] args)
		{
			try
			{
				System.Console.WriteLine();
				System.Console.WriteLine("] Peach Farm - Admin Client");
				System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
				System.Console.WriteLine();

				bool start = false;
				bool stop = false;
				bool list = false;
				bool errors = false;
				bool jobInfo = false;

				string tagsString = String.Empty;
				int launchCount = 0;
				string ip = String.Empty;

				#region Parse & Validate Console Input
				var p = new OptionSet()
					{
						{ "?|help", v => syntax() },
					
						// Commands
						{ "start", v => start = true },
						{ "stop", v => stop = true },
						{ "list", v => list = true},
						{ "errors", v => errors = true },
						{ "info", var => jobInfo = true },

						// Command parameters
						{ "n|count=", v => launchCount = int.Parse(v)},
						{ "i|ip=", v => ip = v},
						{ "t|tags=", v => tagsString = v}
					};

				List<string> extra = p.Parse(args);

				if (!stop && !start && !list && !errors && !jobInfo)
					Program.syntax();

				if (start && launchCount == 0 && String.IsNullOrEmpty(tagsString) && String.IsNullOrEmpty(ip))
					Program.syntax();

				if (start && extra.Count < 1)
					Program.syntax();

				if (stop && extra.Count != 1)
					Program.syntax();

				if (jobInfo && extra.Count != 1)
					Program.syntax();

				#endregion

				#region Set up Admin listener

				admin = new Admin();
				
				admin.ListNodesCompleted += admin_ListNodesCompleted;
				admin.ListErrorsCompleted += admin_ListErrorsCompleted;
				admin.StartPeachCompleted += admin_StartPeachCompleted;
				admin.StopPeachCompleted += admin_StopPeachCompleted;
				admin.JobInfoCompleted += admin_JobInfoCompleted;
				admin.AdminException += new EventHandler<Admin.ExceptionEventArgs>(admin_AdminException);
				#endregion

				#region Start
				if (start)
				{
					string pitFilePath = extra[0];

					string definesFilePath = String.Empty;
					if (extra.Count >= 2)
						definesFilePath = extra[1];

					admin.StartPeachAsync(pitFilePath, definesFilePath, launchCount, tagsString, ip);
				}
				#endregion

				#region Stop
				if (stop)
				{
					admin.StopPeachAsync(extra[0]);

					/*
					if (extra.Count > 0)
					{
						admin.StopPeachAsync(extra[0]);
					}
					else
					{
						admin.StopPeachAsync();
					}
					//*/
				}
				#endregion

				#region List
				if (list)
				{
					admin.ListNodesAsync();
				}
				#endregion

				#region Errors
				if (errors)
				{
					if (extra.Count > 0)
					{
						string jobID = extra[0];
						admin.ListErrorsAsync(jobID);
					}
					else
					{
						admin.ListErrorsAsync();
					}
				}
				#endregion

				#region Job Info
				if (jobInfo)
				{
					string jobID = extra[0];
					admin.JobInfoAsync(jobID);
				}
				#endregion

				System.Console.WriteLine("waiting for result...");
				System.Console.ReadLine();
			}
			catch (SyntaxException)
			{
				PrintHelp();
			}
			catch (ApplicationException aex)
			{
				System.Console.WriteLine(aex.Message);
			}
			catch (Exception)
			{
				PrintHelp();
			}

			Environment.Exit(0);
		}

		#region Peach Farm Admin message completion handlers
		static void admin_StopPeachCompleted(object sender, Admin.StopPeachCompletedEventArgs e)
		{
			if (e.Result.Success)
			{
				System.Console.WriteLine("Stop Peach Success\n");
			}
			else
			{
				System.Console.WriteLine(String.Format("Stop Peach Failure\n{0}", e.Result.Message));
			}
		}

		static void admin_StartPeachCompleted(object sender, Admin.StartPeachCompletedEventArgs e)
		{
			if (e.Result.Success)
			{
				System.Console.WriteLine(String.Format("Start Peach Success\nUser Name: {0}\nPit File: {1}\nJob ID: {2}", e.Result.UserName, e.Result.PitFileName, e.Result.JobID));
			}
			else
			{
				System.Console.WriteLine(String.Format("Start Peach Failure\n{0}", e.Result.Message));
			}
		}

		static void admin_ListErrorsCompleted(object sender, Admin.ListErrorsCompletedEventArgs e)
		{
			if (e.Result.Nodes.Count > 0)
			{
				foreach (Heartbeat heartbeat in e.Result.Nodes)
				{
					System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\n{3}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.ErrorMessage));
				}
			}
			else
			{
				System.Console.WriteLine("Response: No errors recorded.");
			}
		}

		static void admin_ListNodesCompleted(object sender, Admin.ListNodesCompletedEventArgs e)
		{
			if (e.Result.Nodes.Count > 0)
			{
				foreach (Heartbeat heartbeat in e.Result.Nodes)
				{
					if (heartbeat.Status == Status.Running)
					{
						System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.JobID));
					}
					else
					{
						System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp, heartbeat.Tags));
					}
				}


				System.Console.WriteLine();

				var statusgroup = from c in e.Result.Nodes where c.Status == Status.Alive select c;
				System.Console.WriteLine(String.Format("Waiting for work: {0}", statusgroup.Count()));

				statusgroup = from c in e.Result.Nodes where c.Status == Status.Running select c;
				System.Console.WriteLine(String.Format("Running: {0}", statusgroup.Count()));

				statusgroup = from c in e.Result.Nodes where c.Status == Status.Late select c;
				System.Console.WriteLine(String.Format("Late: {0}", statusgroup.Count()));
			}
			else
			{
				System.Console.WriteLine("Response: No nodes online");
			}
		}

		static void admin_JobInfoCompleted(object sender, Admin.JobInfoCompletedEventArgs e)
		{
			string output = String.Format("JobID:\t\t{0}\nUser Name:\t{1}\nPit Name:\t{2}\nStart Date:\t{3}\n\nRunning Nodes:",
				e.Result.Job.JobID,
				e.Result.Job.UserName,
				e.Result.Job.PitFileName,
				e.Result.Job.StartDate);

			System.Console.WriteLine(output);

			foreach (Heartbeat heartbeat in e.Result.Nodes)
			{
				System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.JobID));
			}
		}

		static void admin_AdminException(object sender, Admin.ExceptionEventArgs e)
		{
			System.Console.WriteLine(e.Exception.ToString());
		}

		#endregion


		#region PrintHelp
		static void PrintHelp()
		{
			//  pf_admin.exe -logs destinationpath
			//  pf_admin.exe -logs destinationpath commandLineSearch
			//  commandLine - Peach command line to execute
			//  logs   - Pull down logs for all runs or specific runs
			//  destinationpath   - Path to save logs to locally
			//  commandLineSearch - Full or partial command line to match
			//         pf_admin.exe -stop

			System.Console.WriteLine(@"

 pf_admin.exe is the admin interface for Peach Farm.  All Peach Farm
 functions can be controlled via this tool.

Syntax Guide
------------

Syntax: 
      Start Peach on the first <clientCount> number of Alive Nodes:
        pf_admin.exe -start -n clientCount pitFilePath <definesFilePath>
      
      Start Peach on the first <clientCount> number of Alive Nodes with matching <tags>:
        pf_admin.exe -start -n clientCount -t tags pitFilePath <definesFilePath>

      Start Peach on a single specific Node:
        pf_admin.exe -start --ip ipAddress pitFilePath <definesFilePath>

      Start Peach on all Alive nodes matching <tags>:
        pf_admin.exe -start -t tags pitFilePath <definesFilePath>
    
      Stop Peach on Nodes matching <jobID>:
        pf_admin.exe -stop jobID
      
      Get list of all Nodes:
        pf_admin.exe -list

      Get list of errors reported by Nodes:
        pf_admin.exe -errors

      Get list of errors reported by Nodes for Job <jobID>
        pf_admin.exe -errors jobID

      Get information for a Job and a list of Running Nodes
        pf_admin.exe -info jobID


Commands:

 start   - Start one or more instances of Peach.
   clientCount - Number of instances to start
   tags        - Comma delimited list of tags to match nodes with

   ipAddress   - Address of specific node to launch on

   pitFilePath - Full path to Pit File
   definesFilePath - Full path to Defines File (optional)

 stop   - Stop some instances of Peach
   jobID - Job ID

 list   - List all nodes in the farm with status

 errors - List any logged node errors
   jobID - Job ID

 info - Get information for a Job and a list of Running Nodes
   jobID - Job ID

");
		}
		#endregion

	}

	public class SyntaxException : Exception
	{
	}
}
