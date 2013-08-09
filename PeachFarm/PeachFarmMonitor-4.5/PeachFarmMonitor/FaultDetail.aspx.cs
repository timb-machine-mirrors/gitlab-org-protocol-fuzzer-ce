using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using MongoDB.Bson;
using PeachFarm.Common.Mongo;
using PeachFarmMonitor.Configuration;
using System.Configuration;
using PeachFarmMonitor.ViewModels;

namespace PeachFarmMonitor
{
	public partial class FaultDetail : System.Web.UI.Page
	{

		public FaultDetail()
		{
		}

		protected void Page_Load(object sender, EventArgs e)
		{
		}
	}

	public static class FaultData
	{
		public static List<FaultViewModel> GetFaultData(string faultid)
		{
			PeachFarmMonitorSection monitorconfig = (PeachFarmMonitorSection)ConfigurationManager.GetSection("peachfarmmonitor");
			var faultscollection = DatabaseHelper.GetCollection<Fault>(MongoNames.Faults, monitorconfig.MongoDb.ConnectionString);
			var fault = faultscollection.FindOneById(new BsonObjectId(faultid));
			faultscollection.Database.Server.Disconnect();
			FaultViewModel fvm = new FaultViewModel(fault);
			fvm.GetFullTextForGeneratedFiles();
			return new List<FaultViewModel>(){fvm};
		}
	}
}