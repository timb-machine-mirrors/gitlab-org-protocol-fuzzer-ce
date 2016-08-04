using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace PeachDownloader
{
	public partial class License : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void AcceptLicense_Click(object sender, EventArgs e)
		{
			Session["AcceptLicense"] = true;
			Response.Redirect("Downloads.aspx?p="+Server.UrlEncode(Request["p"])+
				"&b="+Server.UrlEncode(Request["b"])+
				"&f="+Server.UrlEncode(Request["f"]), true);
		}

		protected void Fail_Click(object sender, EventArgs e)
		{
			Response.Redirect("Default.aspx", true);
		}
	}
}