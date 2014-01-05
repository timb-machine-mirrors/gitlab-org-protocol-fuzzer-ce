using System;
using System.Collections.Generic;
using System.IO;

namespace PeachFarmMonitor
{
    public static class Extensions
    {
        public static string GetTempFile(this System.Web.UI.Page page)
        {
            string temppath = Path.GetTempFileName();
            ((List<string>)page.Session["tempfiles"]).Add(temppath);
            return temppath;
        }
    }
}