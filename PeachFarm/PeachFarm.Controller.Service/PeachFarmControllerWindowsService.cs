using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Net;
using NLog;

namespace PeachFarm.Controller
{
  partial class PeachFarmControllerWindowsService : ServiceBase
  {

    private static PeachFarmServer server = null;

    private static Logger logger = LogManager.GetCurrentClassLogger();

    public PeachFarmControllerWindowsService()
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
      try
      {
        server = new PeachFarmServer();
        logger.Info("Peach Farm Controller Started.");
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

    protected override void OnStop()
    {
      if ((server != null) && (server.IsOpen))
      {
        server.Close();
        logger.Info("Peach Farm Controller Stopped.");
      } 
    }
  }
}
