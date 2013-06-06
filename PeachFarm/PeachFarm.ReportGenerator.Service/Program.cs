using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace PeachFarm.Reporting.Service
{
  static class Program
  {

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      if (Environment.UserInteractive)
      {
        System.Console.WriteLine();
        System.Console.WriteLine("] Peach Farm - Report Generator");
        System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
        System.Console.WriteLine();

        #region initializing and starting node

				#endregion

				ReportGenerator reportGenerator = new ReportGenerator();

        #region press enter to close gracefully
        System.Console.WriteLine("Peach Farm Report Generator online. Listening. Press Enter to exit gracefully.");
        System.Console.ReadLine();
        #endregion

        #region stopping node
        reportGenerator.Close();
        #endregion
      }
      else
      {
        ServiceBase[] ServicesToRun;
        ServicesToRun = new ServiceBase[] 
			  { 
				  new ReportGeneratorService() 
			  };
        ServiceBase.Run(ServicesToRun);
      }
    }
  }
}
