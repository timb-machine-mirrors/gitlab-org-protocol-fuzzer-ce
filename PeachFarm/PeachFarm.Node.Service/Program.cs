using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;

namespace PeachFarm.Node.Service
{
  static class Program
  {
    private static PeachFarmNode peachFarmNode = null;

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      if (Environment.UserInteractive)
      {
        System.Console.WriteLine();
        System.Console.WriteLine("] Peach Farm - Node");
        System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
        System.Console.WriteLine();

        #region initializing and starting node
        peachFarmNode = new PeachFarmNode();
        peachFarmNode.StatusChanged += new EventHandler<StatusChangedEventArgs>(peachFarmNode_StatusChanged);
        #endregion

        #region press enter to close gracefully
        System.Console.WriteLine("Peach Farm Node connected to " + peachFarmNode.ServerQueue + " successfully. Listening. Press Enter to exit gracefully.");
        System.Console.ReadLine();
        #endregion

        #region stopping node
        peachFarmNode.Close();
        #endregion
      }
      else
      {
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[] 
			  { 
				  new PeachFarmNodeWindowsService() 
			  };
        ServiceBase.Run(ServicesToRun);
      }
    }

    static void peachFarmNode_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      System.Console.WriteLine("Status changed: " + e.Status.ToString());
    }

  }
}
