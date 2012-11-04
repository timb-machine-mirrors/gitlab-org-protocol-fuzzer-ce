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
        bool logs = false;
        string hostName = "localhost";

        int launchCount = -1;
        string ip = null;

        var p = new OptionSet()
					{
						{ "?|help", v => syntax() },
					
						// Commands
						{ "start", v => start = true },
						{ "stop", v => stop = true },
						{ "list", v => list = true},
						{ "errors", v => errors = true },
						{ "logs", v => logs = true},

						// Command parameters
						{ "h|host=", v => hostName = v},
						{ "n|count=", v => launchCount = int.Parse(v)},
						{ "i|ip=", v => ip = v}

					};

        List<string> extra = p.Parse(args);

        if (!stop && !start && !list && !errors && !logs)
          throw new SyntaxException();

        if (start && extra.Count != 2)
          throw new SyntaxException();

        Admin admin = new Admin(hostName);
        try
        {
          admin.StartAdmin();
        }
        catch (ApplicationException aex)
        {
          System.Console.WriteLine(aex.Message);
          return;
        }

        admin.ListComputersCompleted += admin_ListComputersCompleted;
        admin.ListErrorsCompleted += admin_ListErrorsCompleted;
        admin.StartPeachCompleted += admin_StartPeachCompleted;
        admin.StopPeachCompleted += admin_StopPeachCompleted;

        List<string> computerNames = new List<string>();

        if (start)
        {
          string commandLine = extra[0];
          string logPath = extra[1];

          if (launchCount > 0)
          {
            admin.StartPeachAsync(commandLine, launchCount, logPath);
          }
          else
          {
            admin.StartPeachAsync(commandLine, ip, logPath);
          }
        }

        if (stop)
        {
          if (extra.Count > 0)
          {
            admin.StopPeachAsync(extra[0]);
          }
          else
          {
            admin.StopPeachAsync();
          }
        }

        if (list)
        {
          admin.ListComputersAsync();
        }

        if (errors)
        {
          admin.ListErrorsAsync();
        }

        if (logs)
        {
          string connectionString = extra[0];
          string destinationPath = extra[1];

          if (extra.Count >= 2)
          {
            string commandLineSearch = extra[2];
            GetFiles(connectionString, destinationPath, commandLineSearch);
          }
          else
          {
            GetFiles(connectionString, destinationPath);
          }
        }

        System.Console.WriteLine("waiting for result...");
        System.Console.ReadLine();

        admin.StopAdmin();
      }
      catch (SyntaxException)
      {
        PrintHelp();
      }
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
        System.Console.WriteLine("Start Peach Success\n");
      }
      else
      {
        System.Console.WriteLine(String.Format("Start Peach Failure\n{0}", e.Result.Message));
      }
    }

    static void admin_ListErrorsCompleted(object sender, Admin.ListErrorsCompletedEventArgs e)
    {
      if (e.Result.Computers.Count > 0)
      {
        foreach (Heartbeat heartbeat in e.Result.Computers)
        {
          System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\n{3}", heartbeat.ComputerName, heartbeat.Status.ToString(), heartbeat.Stamp, heartbeat.ErrorMessage));
        }
      }
      else
      {
        System.Console.WriteLine("Response: No errors recorded.");
      }
    }

    static void admin_ListComputersCompleted(object sender, Admin.ListComputersCompletedEventArgs e)
    {
      if (e.Result.Computers.Count > 0)
      {
        foreach (Heartbeat heartbeat in e.Result.Computers)
        {
          System.Console.WriteLine(String.Format("{0}\t{1}\t{2}", heartbeat.ComputerName, heartbeat.Status.ToString(), heartbeat.Stamp));
        }


        System.Console.WriteLine();

        var statusgroup = from c in e.Result.Computers where c.Status == Status.Alive select c;
        System.Console.WriteLine(String.Format("Waiting for work: {0}", statusgroup.Count()));

        statusgroup = from c in e.Result.Computers where c.Status == Status.Running select c;
        System.Console.WriteLine(String.Format("Running: {0}", statusgroup.Count()));

        statusgroup = from c in e.Result.Computers where c.Status == Status.Late select c;
        System.Console.WriteLine(String.Format("Late: {0}", statusgroup.Count()));
      }
      else
      {
        System.Console.WriteLine("Response: No clients online");
      }
    }
    #endregion

    private static void GetFiles(string connectionString, string destinationPath, string commandLineSearch = "")
    {
      if (Directory.Exists(destinationPath) == false)
      {
        try
        {
          Directory.CreateDirectory(destinationPath);
        }
        catch (Exception ex)
        {
          System.Console.WriteLine(String.Format("Could not create folder at {0}, exiting now.\n{1}", destinationPath, ex.Message));
          return;
        }
      }

      try
      {
        DatabaseHelper.GetFilesFromDatabase(connectionString, destinationPath, commandLineSearch);
      }
      catch
      {
        System.Console.WriteLine(String.Format("Could not connect to MongoDB at {0}, exiting now.", connectionString));
        return;
      }

      System.Console.WriteLine("done writing files");
    }

    #region PrintHelp
    static void PrintHelp()
    {

      System.Console.WriteLine(@"

 pf_admin.exe is the admin interface for Peach Farm.  All Peach Farm
 functions can be controlled via this tool.

Syntax Guide
------------

Syntax: pf_admin.exe -start -n clientCount commandLine logPath
        pf_admin.exe -start --ip ipAddress commandLine logPath
        pf_admin.exe -stop
        pf_admin.exe -stop commandLine
        pf_admin.exe -list
        pf_admin.exe -errors
        pf_admin.exe -logs destinationpath
        pf_admin.exe -logs destinationpath commandLineSearch

Optional Arguments:
 
 -h host - Provide the IP or hostname of the controller.  Defaults to
           localhost (127.0.0.1).

Commands:

 start   - Start one or more instances of Peach.
   clientCount - Number of instances to start
   commandLine - Peach command line to execute
   logPath     - Folder containing logs to save
   ipAddress   - Address of specific node to launch on

 stop   - Stop some or all instances of Peach
   commandLine - Full or partial command line to match

 list   - List all nodes in our cluster with status

 errors - List any logged node errors

 logs   - Pull down logs for all runs or specific runs
   destinationpath   - Path to save logs to locally
   commandLineSearch - Full or partial command line to match

Command Line Search
-------------------

 Anytime a Peach instance is started a command line is provided.  This
 command line can be loosly considered the job name.  When running other
 commands we can specify part or all of that command line to match on. 
 As such we can operate on the entire 'job' instead of just single 
 nodes in our cluster.


");
    }
    #endregion

  }

  public class SyntaxException : Exception
  {
  }
}
