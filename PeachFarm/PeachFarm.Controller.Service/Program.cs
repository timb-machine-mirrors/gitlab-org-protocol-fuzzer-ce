using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Diagnostics;
using NLog;
using System.Threading;
using PeachFarm.Common;

namespace PeachFarm.Controller
{
  static class Program
  {
		private static EventWaitHandle waitHandle;
    private static Logger logger = LogManager.GetCurrentClassLogger();
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {

			if (!PeachFarm.Common.Utilities.IsService)
      {
				waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				System.Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);


				System.Console.WriteLine();
				System.Console.WriteLine("] Peach Farm - Controller");
				System.Console.WriteLine("] Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
				System.Console.WriteLine("] Copyright (c) Deja vu Security\n");
				System.Console.WriteLine();

				try
				{
					using (var server = new PeachFarmController())
					{
						if (server.IsListening)
						{
							System.Console.WriteLine(String.Format("Peach Farm Controller ({0}) waiting for messages", server.Name));
							waitHandle.WaitOne();
						}
					}
				}
				catch (System.Configuration.ConfigurationErrorsException ceex)
				{
					Console.WriteLine(ceex.Message);
					Environment.Exit(1);
				}
				catch (RabbitMqException rex)
				{
					Console.WriteLine("Could not communicate with RabbitMQ server at {0}, quitting.", rex.RabbitMqHost);
					Environment.Exit(1);
				}
				catch (ApplicationException aex)
				{
					Console.WriteLine("Application Exception:\n{0}", aex.Message);
					Environment.Exit(1);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Unknown/Unhandled Exception\n{0}", ex.Message);
					Environment.Exit(1);
				}

				Environment.Exit(0);
      }
      else
      {
	      try
	      {
					PeachFarmControllerWindowsService service = new PeachFarmControllerWindowsService();
					ServiceBase[] ServicesToRun;
					ServicesToRun = new ServiceBase[] { service };
					ServiceBase.Run(ServicesToRun);
				}
	      catch (Exception ex)
	      {
					logger.Fatal(ex.ToString());
	      }
      }
    }

		private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			if (waitHandle != null)
			{
				waitHandle.Set();
				e.Cancel = true;
			}
		}

  }
}
