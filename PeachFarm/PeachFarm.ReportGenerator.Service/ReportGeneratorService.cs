using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace PeachFarm.Reporting.Service
{
	public partial class ReportGeneratorService : ServiceBase
	{
		private PeachFarm.Reporting.ReportGenerator reportGenerator = null;

		public ReportGeneratorService()
		{
			InitializeComponent();
		}

		protected override void OnStart(string[] args)
		{
			reportGenerator = new PeachFarm.Reporting.ReportGenerator();
		}

		protected override void OnStop()
		{
			reportGenerator.Close();
		}
	}
}
