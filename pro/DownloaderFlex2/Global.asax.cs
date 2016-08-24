using System;
using System.Configuration;

namespace PeachDownloader
{
	public class Global : System.Web.HttpApplication
	{
		protected void Application_Start(object sender, EventArgs e)
		{
			Application[SessionKeys.Downloads] = ConfigurationManager.AppSettings["Downloads"];
		}

		protected void Session_Start(object sender, EventArgs e)
		{
			Session[SessionKeys.Operations] = null;
			Session[SessionKeys.Authenticated] = false;
			Session[SessionKeys.AcceptLicense] = false;

			// Old
			Session["AcceptLicense"] = false;
			Session["LicenseValidation"] = false;
			Session["LicenseEnt"] = false;
			Session["LicensePro"] = false;
		}

		protected void Application_BeginRequest(object sender, EventArgs e)
		{
		}

		protected void Application_AuthenticateRequest(object sender, EventArgs e)
		{
		}

		protected void Application_Error(object sender, EventArgs e)
		{
		}

		protected void Session_End(object sender, EventArgs e)
		{
		}

		protected void Application_End(object sender, EventArgs e)
		{
		}
	}
}
