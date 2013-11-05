using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common;
using System.IO;
using PeachFarm.Common.Messages;

namespace PeachFarm.Admin
{
	public class Program
	{
		private static PeachFarmAdmin admin = null;

		static void syntax() { throw new SyntaxException(); }

		static void Main(string[] args)
		{
			try
			{
				System.Console.WriteLine();
				System.Console.WriteLine("] Peach Farm - Admin Client");
				System.Console.WriteLine("] Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
				System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
				System.Console.WriteLine();

				bool start = false;
				bool stop = false;
				bool nodes = false;
				bool errors = false;
				bool jobInfo = false;
				bool jobs = false;
				bool output = false;
				bool truncate = false;
				bool report = false;
				bool web = false;

				string tagsString = String.Empty;
				int launchCount = 0;
				string ip = String.Empty;

				string range = String.Empty;

				#region Parse & Validate Console Input
				var p = new OptionSet()
					{
						{ "?|help", v => syntax() },
					
						// Commands
						{ "start", v => start = true },
						{ "stop", v => stop = true },
						{ "nodes", v => nodes = true},
						{ "errors", v => errors = true },
						{ "info", v => jobInfo = true },
						{ "jobs", v => jobs = true },
						{ "output", v => output = true},
						{ "truncate", v => truncate = true},
						{ "report", v => report = true},
						{ "web", v => web = true},

						// Command parameters
						{ "n|count=", v => launchCount = int.Parse(v)},
						{ "i|ip=", v => ip = v},
						{ "t|tags=", v => tagsString = v},
						{ "r|range=", v => range = v}
					};

				List<string> extra = p.Parse(args);

				if (!stop && !start && !nodes && !errors && !jobInfo && !jobs && !output && !truncate && !report && !web)
					Program.syntax();

				if (start && launchCount == 0 && String.IsNullOrEmpty(tagsString) && String.IsNullOrEmpty(ip))
					Program.syntax();

				if (start && extra.Count < 1)
					Program.syntax();

				if (stop && extra.Count != 1)
					Program.syntax();

				if (jobInfo && extra.Count != 1)
					Program.syntax();

				if (output && extra.Count != 2)
					Program.syntax();

				if (report && extra.Count != 1)
				{
					//
				}

				#endregion

				bool mustwait = true;

				#region Set up Admin listener

				admin = new PeachFarmAdmin();
				//ServiceHost host = null;

				admin.StartPeachCompleted += admin_StartPeachCompleted;
				admin.StopPeachCompleted += admin_StopPeachCompleted;

				admin.AdminException += new EventHandler<PeachFarm.Admin.PeachFarmAdmin.ExceptionEventArgs>(admin_AdminException);
				#endregion

				#region Start
				if (start)
				{
					if (launchCount < 0)
					{
						System.Console.WriteLine(String.Format("{0} is not a quantity of machines. Try a positive number. 0 will be treated as All machines.", launchCount));
						return;
					}

					string pitFilePath = extra[0];

					string definesFilePath = String.Empty;
					string target = String.Empty;
					if(extra.Count >= 3)
					{
						definesFilePath = extra[1];
						target = extra[2];
					}
					else if (extra.Count == 2)
					{
						if(pitFilePath.EndsWith(".zip") && File.Exists(extra[1]))
						{
							definesFilePath = extra[1];
						}
						else
						{
							target = extra[1];
						}
					}

					#region range
					uint? rangestart = null;
					uint? rangeend = null;

					if(String.IsNullOrEmpty(range) == false)
					{
						try
						{
							var rangeparsed = range.Split('-');
							if (rangeparsed.Length == 2)
							{
								rangestart = uint.Parse(rangeparsed[0]);
								rangeend = uint.Parse(rangeparsed[1]);
								if (rangeend < rangestart)
								{
									rangestart = null;
									rangeend = null;
								}
							}
						}
						catch
						{
							Program.syntax();
							return;
						}
					}
					#endregion

					admin.StartPeachAsync(pitFilePath, definesFilePath, launchCount, tagsString, ip, target, rangestart, rangeend);
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
				if (nodes)
				{
					mustwait = false;
					PrintListNodes(admin.ListNodes());
				}
				#endregion

				#region Errors
				if (errors)
				{
					mustwait = false;
					if (extra.Count > 0)
					{
						string jobID = extra[0];
						PrintListErrors(admin.ListErrors(jobID));
					}
					else
					{
						PrintListErrors(admin.ListErrors());
					}
				}
				#endregion

				#region Job Info
				if (jobInfo)
				{
					mustwait = false;
					string jobID = extra[0];
					PrintJobInfo(admin.JobInfo(jobID));
				}
				#endregion

				#region Jobs
				if (jobs)
				{
					mustwait = false;
					PrintMonitor(admin.Monitor());
				}
				#endregion

				#region Job Output
				if (output)
				{
					mustwait = false;

					string jobID = extra[0];
					string destinationFolder = extra[1];

					try
					{
						admin.DumpFiles(jobID, destinationFolder);
					}
					catch (Exception ex)
					{
						System.Console.WriteLine("Error getting files:\n" + ex.Message);
						return;
					}
					System.Console.WriteLine("Done!");
				}
				#endregion
#if DEBUG
				#region Truncate
				if (truncate)
				{
					mustwait = false;
					try
					{
						admin.TruncateAllCollections();
					}
					catch (Exception ex)
					{
						System.Console.WriteLine("Error truncating tables:\n" + ex.ToString());
						return;
					}
					System.Console.WriteLine("Done!");
				}
				#endregion

				#region Report
				if (report)
				{
					string jobid = extra[0];
					admin.Report(jobid);
					mustwait = false;
				}

				#endregion

				#region Listen
				if (web)
				{

				}
				#endregion
#endif
				if (mustwait)
				{
					System.Console.WriteLine("waiting for result...");
					System.Console.ReadLine();
				}

				//if ((host != null) && (host.State == CommunicationState.Opened))
				//{
				//  host.Close();
				//}
			}
			catch (RabbitMqException rex)
			{
				System.Console.WriteLine("Could not communicate with RabbitMQ server at " + rex.RabbitMqHost);
			}
			catch (SyntaxException)
			{
				PrintHelp();
			}
			catch (Exception ex)
			{
				System.Console.WriteLine(ex.Message);
			}


			Environment.Exit(0);
		}



		#region Peach Farm Admin message completion handlers
		static void admin_StopPeachCompleted(object sender, PeachFarm.Admin.PeachFarmAdmin.StopPeachCompletedEventArgs e)
		{
			if (e.Result.Success)
			{
				System.Console.WriteLine("Stop Peach Success\n");
			}
			else
			{
				System.Console.WriteLine(String.Format("Stop Peach Failure\n{0}", e.Result.ErrorMessage));
			}
		}

		static void admin_StartPeachCompleted(object sender, PeachFarm.Admin.PeachFarmAdmin.StartPeachCompletedEventArgs e)
		{
			if (e.Result.Success)
			{
				System.Console.WriteLine(String.Format("Start Peach Success\nUser Name: {0}\nPit File: {1}\nJob ID: {2}", e.Result.UserName, e.Result.PitFileName, e.Result.JobID));
			}
			else
			{
				System.Console.WriteLine(String.Format("Start Peach Failure\n{0}", e.Result.ErrorMessage));
			}
		}

		private static void PrintListErrors(ListErrorsResponse e)
		{
			if (e.Errors.Count > 0)
			{
				foreach (Heartbeat heartbeat in e.Errors)
				{
					System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\n{3}", heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.ErrorMessage));
				}
			}
			else
			{
				System.Console.WriteLine("Response: No errors recorded.");
			}
		}

		private static void PrintListNodes(ListNodesResponse e)
		{
			if (e.Nodes.Count > 0)
			{
				string format = "{0,-16}{1,-8}{2,-25}{3,-13}{4}";
				System.Console.WriteLine(format, "Name", "Status", "Stamp", "Version", "JobID/Tags");
				foreach (Heartbeat heartbeat in e.Nodes)
				{
					if(string.IsNullOrEmpty(heartbeat.Version))
					{
						heartbeat.Version = "(Dev Build)";
					}

					if (heartbeat.Status == Status.Running)
					{
						System.Console.WriteLine(String.Format(format, heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.Version, heartbeat.JobID));
					}
					else
					{
						System.Console.WriteLine(String.Format(format, heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.Version, heartbeat.Tags));
					}
				}


				System.Console.WriteLine();

				var statusgroup = from c in e.Nodes where c.Status == Status.Alive select c;
				System.Console.WriteLine(String.Format("Waiting for work: {0}", statusgroup.Count()));

				statusgroup = from c in e.Nodes where c.Status == Status.Running select c;
				System.Console.WriteLine(String.Format("Running: {0}", statusgroup.Count()));

				statusgroup = from c in e.Nodes where c.Status == Status.Late select c;
				System.Console.WriteLine(String.Format("Late: {0}", statusgroup.Count()));
			}
			else
			{
				System.Console.WriteLine("Response: No nodes online");
			}
		}

		private static void PrintJobInfo(JobInfoResponse e)
		{
			if (e.Success)
			{
				string output = String.Format("JobID:\t{0}\nUser Name:\t{1}\nPit Name:\t{2}\nStart Date:\t{3}\nIterations:\t{4}\n\nRunning Nodes:",
					e.Job.JobID,
					e.Job.UserName,
					e.Job.Pit.FileName,
					e.Job.StartDate,
					e.Nodes.Sum((h) => h.Iteration));

				System.Console.WriteLine(output);

				string format = "{0,-16}{1,-8}{2,-25}{3,-13}{4,-17}";
				System.Console.WriteLine(format,"Name","Status","Last Updated", "Job ID", "Iterations");
				System.Console.WriteLine(format,"---------------","-------","------------------------","------------","----------------");
				foreach (Heartbeat heartbeat in e.Nodes)
				{
					System.Console.WriteLine(String.Format(format, heartbeat.NodeName, heartbeat.Status.ToString(), heartbeat.Stamp.ToLocalTime(), heartbeat.JobID, heartbeat.Iteration));
				}
			}
			else
			{
				System.Console.WriteLine("Job not found: " + e.Job.JobID);
			}
		}

		static void PrintMonitor(MonitorResponse e)
		{
			string format = "{0,-13}{1,-21}{2,-21}{3,-24}";
			System.Console.WriteLine("Active Jobs");
			System.Console.WriteLine("-----------");
			if (e.ActiveJobs.Count == 0)
			{
				System.Console.WriteLine("(no active jobs)");
			}
			else
			{
				System.Console.WriteLine(String.Format(format, "Job ID", "User Name", "Pit Name", "Start Date"));
				foreach (Job job in e.ActiveJobs)
				{
					System.Console.WriteLine(String.Format(format,
						job.JobID, job.UserName, job.Pit.FileName, job.StartDate));
				}
			}
			System.Console.WriteLine();
			System.Console.WriteLine("Inactive Jobs");
			System.Console.WriteLine("-------------");

			if (e.InactiveJobs.Count == 0)
			{
				System.Console.WriteLine("(no inactive jobs)");
			}
			else
			{
				System.Console.WriteLine(String.Format(format, "Job ID", "User Name", "Pit Name", "Start Date"));
				foreach (Job job in e.InactiveJobs)
				{
					System.Console.WriteLine(String.Format(format,
						job.JobID, job.UserName, job.Pit.FileName, job.StartDate));
				}
			}
		}

		static void admin_AdminException(object sender, PeachFarm.Admin.PeachFarmAdmin.ExceptionEventArgs e)
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
        pf_admin.exe -nodes

      Get list of errors reported by Nodes:
        pf_admin.exe -errors

      Get list of errors reported by Nodes for Job <jobID>
        pf_admin.exe -errors jobID

			Get information for all Jobs
				pf_admin.exe -jobs

      Get information for a Job and a list of Running Nodes
        pf_admin.exe -info jobID

			Get generated files for a Job
				pf_admin.exe -output jobID destinationFolder

Commands:

 start   - Start one or more instances of Peach.
   clientCount			- Number of instances to start
   tags							- Comma delimited list of tags to match nodes with

   ipAddress				- Address of specific node to launch on

   pitFilePath			- Full path to Pit File or Zip File
   definesFilePath	- Full path to Defines File (optional)

	 range						- Iteration range (optional), format is <start>-<end> example: 1000-2000

 stop   - Stop some instances of Peach
   jobID - Job ID

 nodes   - List all nodes in the farm with status

 errors - List any logged node errors
   jobID - Job ID

 jobs - Get information for all Jobs

 info - Get information for a Job and a list of Running Nodes
   jobID - Job ID

 output - Get generated files for a Job
	 jobID - Job ID
   destinationFolder - Folder where files will be downloaded to

");
		}
		#endregion

	}

	public class SyntaxException : Exception
	{
	}
}
