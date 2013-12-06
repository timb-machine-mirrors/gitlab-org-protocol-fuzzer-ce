using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Threading;

namespace PeachFarm.Node.Service
{
  static class Program
  {
		private static EventWaitHandle waitHandle;
		private static NLog.Logger nlog = NLog.LogManager.GetCurrentClassLogger();
		/// <summary>
    /// The main entry point for the application.
    /// </summary>
    static void Main()
    {
      if (Environment.UserInteractive)
      {
				waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
				System.Console.CancelKeyPress += new ConsoleCancelEventHandler(Console_CancelKeyPress);

				System.Console.WriteLine();
				System.Console.WriteLine("] Peach Farm - Node");
				System.Console.WriteLine("] Version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());
				System.Console.WriteLine("] Copyright (c) Deja vu Security\n");

				#region initializing and starting node
				try
				{
					using (var peachFarmNode = new PeachFarmNode())
					{
						System.Console.WriteLine();
						peachFarmNode.StatusChanged += new EventHandler<StatusChangedEventArgs>(peachFarmNode_StatusChanged);

						System.Console.WriteLine("Peach Farm Node connected to " + peachFarmNode.ServerQueue + " successfully. Listening.");
						waitHandle.WaitOne();
					}
				}
				catch(System.Configuration.ConfigurationErrorsException ceex)
				{
					Console.WriteLine(ceex.Message);
					Environment.Exit(1);
				}
				catch (PeachFarm.Common.RabbitMqException rex)
				{
					Console.WriteLine("Could not communicate with RabbitMQ server at {0}, quitting.", rex.RabbitMqHost);
					Environment.Exit(1);
				}
				catch (ApplicationException aex)
				{
					Console.WriteLine("Application Exception:\n{0}", aex.Message);
					Environment.Exit(1);
				}

				#endregion

				Environment.Exit(0);
				return;
      }
      else
      {
	      try
	      {
					ServiceBase[] ServicesToRun;
					ServicesToRun = new ServiceBase[] 
					{ 
						new PeachFarmNodeWindowsService() 
					};
					ServiceBase.Run(ServicesToRun);
				}
	      catch (Exception ex)
	      {
		      nlog.Fatal(ex.ToString());
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

    private static void peachFarmNode_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      System.Console.WriteLine("Status changed: " + e.Status.ToString());
    }

  }
}
