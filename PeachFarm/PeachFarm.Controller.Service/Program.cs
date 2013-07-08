using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using NLog;

namespace PeachFarm.Controller
{
  static class Program
  {

    private static Logger logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      if (Environment.UserInteractive)
      {
        System.Console.WriteLine();
        System.Console.WriteLine("] Peach Farm - Controller");
        System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
        System.Console.WriteLine();

        PeachFarmController server = null;
        try
        {
          server = new PeachFarmController();
          if (server.IsListening)
          {
            System.Console.WriteLine(String.Format("Peach Farm Server ({0}) waiting for messages", server.QueueName));
            System.Console.ReadLine();
          }
          server.Close();
        }
        catch (ApplicationException aex)
        {
          logger.Fatal("Application Exception:\n{0}", aex.Message);
        }
        catch (Exception ex)
        {
          logger.Fatal("Unknown/Unhandled Exception\n{0}", ex.Message);
        }
        finally
        {
          if ((server != null))
          {
            server.Close();
          }
        }
      }
      else
      {
        PeachFarmControllerWindowsService service = new PeachFarmControllerWindowsService();
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[] { service };
        ServiceBase.Run(ServicesToRun);
      }
    }
  }
}
