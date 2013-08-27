using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

using NLog;

namespace PeachFarm.Reporting.Service
{
	public partial class ReportGeneratorService : ServiceBase
	{
		private PeachFarm.Reporting.ReportGenerator reportGenerator = null;
		private static Logger logger = LogManager.GetCurrentClassLogger();

		public ReportGeneratorService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			try
			{
				reportGenerator = new PeachFarm.Reporting.ReportGenerator();
				logger.Info("Peach Farm Reporting Service Started");
			}
			catch (Exception ex)
			{
				logger.Fatal("Peach Farm Reporting Service encountered an exception while starting:\n" + ex.Message);

			}
		}

		protected override void OnStop()
		{
			if(reportGenerator != null)
				reportGenerator.Close();

			logger.Info("Peach Farm Reporting Service Stopped");
		}
	}
}
