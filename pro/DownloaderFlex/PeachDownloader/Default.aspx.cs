using System;
using System.IO;

namespace PeachDownloader
{
	public partial class Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		protected void Login_Click(object sender, EventArgs e)
		{
			var operations = new Operations(TextBoxUser.Text, TextBoxPassword.Text);
			var result = operations.ValidateCredentials();

			if (result)
			{
				Session[SessionKeys.Authenticated] = true;
				Session[SessionKeys.Operations] = operations;
				Response.Redirect("Downloads.aspx", true);
			}
			else
			{
				Session[SessionKeys.Authenticated] = false;
				Response.Redirect("Default.aspx?error=true", true);
			}
		}
	}
}

// end
