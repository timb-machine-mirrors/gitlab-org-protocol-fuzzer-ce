﻿using System;
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

        string tagsString = String.Empty;
        string hostName = String.Empty;
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

						// Command parameters
						{ "h|host=", v => hostName = v},
						{ "n|count=", v => launchCount = int.Parse(v)},
						{ "i|ip=", v => ip = v},
            { "t|tags=", v => tagsString = v}
					};

        List<string> extra = p.Parse(args);

        if (String.IsNullOrEmpty(hostName))
          Program.syntax();

        if (!stop && !start && !list && !errors)
          Program.syntax();

        if (start && extra.Count != 1)
          Program.syntax();

        if (stop && extra.Count != 1)
          Program.syntax();

        #endregion

        #region Set up Admin listener
        Admin admin = new Admin(hostName);
        admin.StartAdmin();

        admin.ListComputersCompleted += admin_ListComputersCompleted;
        admin.ListErrorsCompleted += admin_ListErrorsCompleted;
        admin.StartPeachCompleted += admin_StartPeachCompleted;
        admin.StopPeachCompleted += admin_StopPeachCompleted;
        admin.AdminException += new EventHandler<Admin.ExceptionEventArgs>(admin_AdminException);
        #endregion

        #region Start
		    if (start)
		    {
			    string pitFilePath = extra[0];
			    //string logPath = extra[1];
			    admin.StartPeachAsync(pitFilePath, launchCount, tagsString, ip);
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
          admin.ListComputersAsync();
        }
        #endregion

        #region Errors
        if (errors)
        {
          admin.ListErrorsAsync();
        }
        #endregion

        System.Console.WriteLine("waiting for result...");
        System.Console.ReadLine();

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
          if (heartbeat.Status == Status.Running)
          {
            System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", heartbeat.ComputerName, heartbeat.Status.ToString(), heartbeat.Stamp, heartbeat.JobID));
          }
          else
          {
            System.Console.WriteLine(String.Format("{0}\t{1}\t{2}\t{3}", heartbeat.ComputerName, heartbeat.Status.ToString(), heartbeat.Stamp, heartbeat.Tags));
          }
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
        System.Console.WriteLine("Response: No nodes online");
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
        pf_admin.exe -h host -start -n clientCount pitFilePath
      
      Start Peach on the first <clientCount> number of Alive Nodes with matching <tags>:
        pf_admin.exe -h host -start -n clientCount -t tags pitFilePath

      Start Peach on a single specific Node:
        pf_admin.exe -h host -start --ip ipAddress pitFilePath

      Start Peach on all Alive nodes matching <tags>:
        pf_admin.exe -h host -start -t tags pitFilePath
    
      Stop Peach on Nodes matching <jobID>:
        pf_admin.exe -h host -stop jobID
      
      Get list of all Nodes:
        pf_admin.exe -list

      Get list of errors reported by Nodes:
        pf_admin.exe -errors

Required Arguments:
 
 -h host - Provide the IP address of the Controller. 
            127.0.0.1, localhost, or other host names are not valid.

Commands:

 start   - Start one or more instances of Peach.
   clientCount - Number of instances to start
   tags        - Comma delimited list of tags to match nodes with

   ipAddress   - Address of specific node to launch on

   pitFilePath - Full path to Pit File

 stop   - Stop some instances of Peach
   jobID - Job ID

 list   - List all nodes in the farm with status

 errors - List any logged node errors

");
    }
    #endregion

  }

  public class SyntaxException : Exception
  {
  }
}
