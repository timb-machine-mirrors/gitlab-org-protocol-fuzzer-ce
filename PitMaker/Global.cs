using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal;
using System.Threading;
using System.DirectoryServices.AccountManagement;
using System.Reflection;
using System.Diagnostics;

namespace PitMaker
{
  internal static class Global
  {
    internal static string GetUserFullName()
    {
      WindowsPrincipal principal = (WindowsPrincipal)Thread.CurrentPrincipal;
      // or, if you're in Asp.Net with windows authentication you can use:
      // WindowsPrincipal principal = (WindowsPrincipal)User;
      using (PrincipalContext pc = new PrincipalContext(ContextType.Domain))
      {
        UserPrincipal up = UserPrincipal.FindByIdentity(pc, principal.Identity.Name);
        //return up.DisplayName;
        return up.GivenName + " " + up.Surname;
      }
    }
    #region old functions
    /*
    #region AllAssemblies Property

    private static List<Assembly> allAssembliesField;

    public static List<Assembly> AllAssemblies
    {
      get
      {
        if (allAssembliesField == null)
        {
          allAssembliesField = AppDomain.CurrentDomain.GetAssemblies().ToList();
          allTypesField = new Dictionary<string, Type>();
          foreach (Assembly assembly in allAssembliesField)
          {
            try
            {
              Type[] assemblytypes = assembly.GetTypes();
              foreach (Type type in assemblytypes)
              {
                if (allTypesField.ContainsKey(type.FullName) == false)
                  allTypesField.Add(type.FullName, type);
              }
            }
            catch (ReflectionTypeLoadException rtlex)
            {
              foreach (Exception ex in rtlex.LoaderExceptions)
              {
                //TODO: Display these loading exceptions
                Debug.WriteLine("Problem loading linked assembly: " + ex.Message);
              }
            }
          }
        }
        return allAssembliesField;
      }
      set
      {
        if (allAssembliesField != value)
        {
          allAssembliesField = value;
        }
      }
    }

    #endregion

    #region AllTypes Property

    private static Dictionary<string, Type> allTypesField;

    public static Dictionary<string, Type> AllTypes
    {
      get
      {
        if (allTypesField == null)
        {
          foreach (Assembly assembly in Global.AllAssemblies)
          {
            try
            {
              Type[] assemblytypes = assembly.GetTypes();
              foreach (Type type in assemblytypes)
              {
                if (allTypesField.ContainsKey(type.FullName) == false)
                  allTypesField.Add(type.FullName, type);
              }
            }
            catch (ReflectionTypeLoadException rtlex)
            {
              foreach (Exception ex in rtlex.LoaderExceptions)
              {
                //TODO: Display these loading exceptions
                Debug.WriteLine("Problem loading linked assembly: " + ex.Message);
              }
            }
          }
        }
        return allTypesField;
      }
      set
      {
        if (allTypesField != value)
        {
          allTypesField = value;
        }
      }
    }

    #endregion
    //*/
    #endregion
  }
}
