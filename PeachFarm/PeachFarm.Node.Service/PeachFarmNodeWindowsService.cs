﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
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

        if (node.Configuration.Output.OutputType == Configuration.OutputType.Console)
        {
          System.Console.WriteLine(String.Format("Unknown/Unhandled Exception\n{0}", ((Exception)e.ExceptionObject).Message));
        }
      }

      Stop();
    }

    protected override void OnStart(string[] args)
    {
      try
      {
        node = new PeachFarmNode();
        node.StartNode();
        logger.Info("Peach Farm Node Started.");
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
      if (node != null)
      {
        node.StopNode();
        logger.Info("Peach Farm Controller Stopped.");
      }
    }
  }
}
