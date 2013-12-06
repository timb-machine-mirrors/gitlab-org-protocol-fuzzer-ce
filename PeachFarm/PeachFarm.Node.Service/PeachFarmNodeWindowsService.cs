using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using NLog;

namespace PeachFarm.Node.Service
{
  public partial class PeachFarmNodeWindowsService : ServiceBase
  {
    private static PeachFarmNode node = null;

    private static Logger logger = LogManager.GetCurrentClassLogger();


    public PeachFarmNodeWindowsService()
    {
      InitializeComponent();
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
    }

    void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
      if (e.ExceptionObject is Exception)
      {
        logger.Fatal("Unknown/Unhandled Exception\n{0}", ((Exception)e.ExceptionObject).Message);
      }

      Stop();
    }

    protected override void OnStart(string[] args)
    {
      base.OnStart(args);

      try
      {
        node = new PeachFarmNode();
        node.StatusChanged += new EventHandler<StatusChangedEventArgs>(node_StatusChanged);
        logger.Info("Peach Farm Node Started. Version: " + node.Version);
      }
			catch (System.Configuration.ConfigurationErrorsException ceex)
			{
				logger.Fatal(ceex.Message);
				Stop();
			}
			catch (ApplicationException aex)
      {
        logger.Fatal("Application Exception:\n{0}", aex.Message);
        Stop();
      }
      catch (Exception ex)
      {
        logger.Fatal("Unknown/Unhandled Exception\n{0}", ex.Message);
        Stop();
      }
    }

    void node_StatusChanged(object sender, StatusChangedEventArgs e)
    {
      logger.Info("Status Changed: " + e.Status.ToString());
    }

    protected override void OnStop()
    {
      if (node != null)
      {
        node.Close();
        logger.Info("Peach Farm Node Stopped.");
      }
			base.OnStop();
		}
  }
}
