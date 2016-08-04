using System;
using System.IO;

namespace PeachDownloader
{
	public partial class Default : System.Web.UI.Page
	{
		protected void Page_Load(object sender, EventArgs e)
		{

		}

		bool VerifyLicense(string xml)
		{
			var linfo = LicenseInfo.Validate(xml);
			if (linfo == null)
				return false;

			Session["LicensePro"] = linfo.Professional;
			Session["LicenseTrial"] = linfo.Trial;
			Session["LicenseEnt"] = linfo.Enterprise;
			Session["Academic"] = linfo.Academic;
			Session["License"] = linfo;
			Session["LicenseXml"] = xml;

			return true;
		}

		protected void Upload_Click(object sender, EventArgs e)
		{
			if ((LicenseFile.PostedFile != null) && (LicenseFile.PostedFile.ContentLength > 0))
			{
				string licenseXml = null;

				using (var sin = new StreamReader(LicenseFile.PostedFile.InputStream))
					licenseXml = sin.ReadToEnd();

				if (VerifyLicense(licenseXml))
				{
					Session["LicenseValidation"] = true;
					Response.Redirect("Downloads.aspx", true);
				}
				else
				{
					Session["LicenseValidation"] = false;
					Response.Redirect("Default.aspx?error=Validation+Failed", true);
				}
			}
			else
			{
				Response.Write("Please select a file to upload.");
			}
		}
	}
}

// end
