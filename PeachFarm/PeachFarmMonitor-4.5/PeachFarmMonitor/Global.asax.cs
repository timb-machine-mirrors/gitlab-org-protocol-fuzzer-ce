using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace PeachFarmMonitor
{
  public class Global : System.Web.HttpApplication
  {
    protected void Application_Start(object sender, EventArgs e)
    {
    }


    protected void Session_Start(object sender, EventArgs e)
    {
      Session["tempfiles"] = new List<string>();
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
      List<string> tempfiles = (List<string>)Session["tempfiles"];
      if (tempfiles != null)
      {
        foreach (var tempfile in tempfiles)
        {
          try
          {
						FileInfo fi = new FileInfo(tempfile);
						if (fi.Exists)
						{
							if (fi.Attributes.HasFlag(FileAttributes.Directory))
								Directory.Delete(tempfile);
							else
								File.Delete(tempfile);
						}
          }
          catch { }
        }
      }
    }

    protected void Application_End(object sender, EventArgs e)
    {

    }
  }
}