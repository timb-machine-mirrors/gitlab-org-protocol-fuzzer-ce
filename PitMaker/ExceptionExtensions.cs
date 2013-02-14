using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace PitMaker
{
  public static class ExceptionExtensions
  {
    public static void WriteAll(this Exception ex, string prefix = "")
    {
      Debug.WriteLine(prefix + ex.Message);
      if (ex.InnerException != null)
      {
        ex.InnerException.WriteAll(prefix + "--");
      }
    }
  }
}
