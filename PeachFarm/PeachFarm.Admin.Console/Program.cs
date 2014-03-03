using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeachFarm.Common;
using System.IO;
using PeachFarm.Common.Messages;
using System.Threading;

namespace PeachFarm.Admin
{
	public class Program
	{
		private static EventWaitHandle waitHandle;

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
				bool report = false;
				bool web = false;
				bool clear = false;

				string tagsString = String.Empty;
				int launchCount = 0;
				string ip = String.Empty;
				string range = String.Empty;
				string target = null;
				string testname = null;
				string definesFilePath = String.Empty;
				uint seed = 0;

				DeleteDataType cleartype = DeleteDataType.Job;
				string clearparameter = String.Empty;

				#region Parse & Validate Console Input
				var p = new OptionSet()
					{
						{ "?|help", v => syntax() },
					
						// Commands
						{ "start", v => start = true },
						{ "stop", v => stop = true },
						{ "nodes", v => nodes = true},
						{ "errors", v => errors = true },
						{ "jobinfo", v => jobInfo = true },
						{ "jobs", v => jobs = true },
						{ "output", v => output = true},
						{ "report", v => report = true},
						{ "clear", v => clear = true},
						{ "web", v => web = true},

						// Command parameters
						{ "n|count=", v => launchCount = int.Parse(v)},
						{ "i|ip=", v => ip = v},
						{ "t|tags=", v => tagsString = v},
						{ "r|range=", v => range = v},
						{ "d|defines=", v => definesFilePath = v},
						{ "a|target=", v => target = v},
						{ "e|test=", v => testname = v},
						{ "s|seed=", v => seed = uint.Parse(v)},
						{ "type=", v => {
							switch(v)
							{
								case "all":
									cleartype = DeleteDataType.All;
									break;
								case "job":
									cleartype = DeleteDataType.Job;
									break;
								case "target":
									cleartype = DeleteDataType.Target;
									break;
								default:
									Program.syntax();
									break;
							}
						}}
					};

				List<string> extra = p.Parse(args);

				if (!stop && !start && !nodes && !errors && !jobInfo && !jobs && !output && !report && !clear && !web)
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

				if(clear && (cleartype == DeleteDataType.Job || cleartype == DeleteDataType.Target) && extra.Count == 0)
					Program.syntax();

				#endregion

				waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				System.Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

				// Set up Admin listener
				using (var admin = new PeachFarmAdmin())
				{
					bool mustwait = false;

					admin.StartPeachCompleted += admin_StartPeachCompleted;
					admin.StopPeachCompleted += admin_StopPeachCompleted;
					admin.DeleteDataCompleted += new EventHandler<PeachFarmAdmin.DeleteDataCompletedEventArgs>(admin_DeleteDataCompleted);
					admin.AdminException += new EventHandler<PeachFarm.Admin.PeachFarmAdmin.ExceptionEventArgs>(admin_AdminException);

					#region Start
					if (start)
					{
						if (launchCount < 0)
						{
							System.Console.WriteLine(String.Format("{0} is not a quantity of machines. Try a positive number. 0 will be treated as All machines.", launchCount));
							return;
						}
						string pitFilePath = extra[0];



						#region range
						uint? rangestart = null;
						uint? rangeend = null;

						if (String.IsNullOrEmpty(range) == false)
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

						mustwait = true;

						admin.StartPeachAsync(pitFilePath, definesFilePath, launchCount, tagsString, ip, target, rangestart, rangeend, testname, seed);
					}
					#endregion

					#region Stop
					if (stop)
					{
						mustwait = true;
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
						PrintListNodes(admin.ListNodes());
					}
					#endregion

					#region Errors
					if (errors)
					{
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
						string jobID = extra[0];
						PrintJobInfo(admin.JobInfo(jobID));
					}
					#endregion

					#region Jobs
					if (jobs)
					{
						mustwait = true;
						PrintMonitor(admin.Monitor());
					}
					#endregion

					#region Job Output
					if (output)
					{
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

					#region Clear
					if (clear)
					{
						if (extra.Count > 0)
						{
							clearparameter = extra[0];
						}
						mustwait = true;
						admin.DeleteDataAsync(cleartype, clearparameter);
					}
					#endregion


					#region Report
					if (report)
					{
						string jobid = extra[0];
						bool reprocess = false;
						if ((extra.Count > 1) && ((extra[1].ToLower() == "reprocess") || (extra[1].ToLower() == "true")))
						{
							reprocess = true;
						}
						admin.Report(jobid, reprocess);
						System.Console.WriteLine("Report request sent.");
					}

					#endregion

#if DEBUG
					#region Listen
					if (web)
					{

					}
					#endregion
#endif
					if (mustwait)
					{
						System.Console.WriteLine("Waiting for response...");
						waitHandle.WaitOne();
					}
				}

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

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			if (waitHandle != null)
			{
				waitHandle.Set();
				e.Cancel = true;
			}
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
			waitHandle.Set();
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
			waitHandle.Set();
		}

		static void admin_DeleteDataCompleted(object sender, PeachFarmAdmin.DeleteDataCompletedEventArgs e)
		{
			if (e.Result.Success)
			{
				System.Console.WriteLine("Delete Data Success");
			}
			else
			{
				System.Console.WriteLine(String.Format("Delete Data Failure\n{0}", e.Result.ErrorMessage));
			}
			waitHandle.Set();
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
			waitHandle.Set();
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
			waitHandle.Set();
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
			waitHandle.Set();
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
			waitHandle.Set();
		}

		static void admin_AdminException(object sender, PeachFarm.Admin.PeachFarmAdmin.ExceptionEventArgs e)
		{
			System.Console.WriteLine(e.Exception.ToString());
			waitHandle.Set();
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

Start Peach full syntax:
pf_admin.exe -start <pitFilePath> 
  -n <clientCount> 
  -t <tags> 
  -i <ipAddress> 
  -d <definesFile> 
  -a <targetName> 
  -r=<range> 
  -test=<testName>
			

Start Peach on the first <clientCount> number of Alive Nodes:
  pf_admin.exe -start -n clientCount pitFile.xml
      
Start Peach on the first <clientCount> number of Alive Nodes 
with matching <tags>:
  pf_admin.exe -start -n clientCount -t tags pitFile.xml

Start Peach on a single specific Node:
  pf_admin.exe -start -ip ipAddress pitFile.xml

Start Peach on all Alive nodes matching <tags>:
  pf_admin.exe -start -t tags pitFile.xml

Start Peach with a defines file
  pf_admin.exe -start -n 1 pitFile.xml -d definesFile.xml

Start Peach with pit, defines, and sample data in a zip file
  pf_admin.exe -start -n 1 zipPackage.zip

Start Peach
    
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

Force a (re)processing of a report
  pf_admin.exe -report jobID [reprocess]

Delete all stored data
  pf_admin.exe -clear -type=all

Delete fault detail for job
  pf_admin.exe -clear -type=job <jobID>

Delete fault detail for all jobs matching target
  pf_admin.exe -clear -type=target <target>

Commands:

start   - Start one or more instances of Peach.
  count(n) - Number of instances to start
  tags(t) - Comma delimited list of tags to match nodes with

  ip(i) - Address of specific node to launch on

  pitFilePath - Full path to Pit File or Zip File

  defines(d) - Full path to Defines File (optional)
  range(r) - Iteration range (optional), 
             format is <start>-<end> example: 1000-2000
  target(a) - target application name (optional)
  test - test name in Pit to run

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
			
#if DEBUG
			Console.ReadLine();
#endif
		}
		#endregion

	}

	public class SyntaxException : Exception
	{
	}
}
