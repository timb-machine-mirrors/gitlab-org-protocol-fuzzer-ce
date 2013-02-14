using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using PitMaker.ViewModels;

namespace PitMaker
{
  internal static class TypeExtensions
  {
    #region old functions
    /*
    public static List<Type> GetClasses(this Type baseType)
    {
      List<Type> derivedtypes;
      derivedtypes = (from type in Global.AllTypes.Values where type.IsSubclassOf(baseType) select type).ToList();
      return derivedtypes;
    }
    public static List<Type> GetTypesWithAttribute(this Type attribute)
    {
      if (attribute.IsSubclassOf(typeof(Attribute)) == false)
        throw new Peach.Core.PeachException(attribute.Name + " is not a valid Attribute type.");

      List<Type> types = new List<Type>();




      foreach (Type assemblytype in Global.AllTypes.Values)
      {
        object[] attributes = assemblytype.GetCustomAttributes(attribute, false);
        if (attributes.Length > 0)
          types.Add(assemblytype);
      }

      return types;
    }
    //*/
    #endregion

    public static List<string> GetBrowsablePropertyNames(this Type type)
    {
      List<string> propertyNames = new List<string>();
      List<PropertyInfo> properties = type.GetProperties().ToList();
      foreach (PropertyInfo info in properties)
      {
        bool browsable = true;
        var attribs = info.GetCustomAttributes(typeof(BrowsableAttribute), false);
        if (attribs.Count() != 0)
        {
          browsable = ((BrowsableAttribute)attribs[0]).Browsable;
        }
        
        if(browsable)
          propertyNames.Add(info.Name);
      }
      return propertyNames;
    }

    public static List<string> GetPropertyNames(this Type type)
    {
      List<string> propertyNames = new List<string>();
      List<PropertyInfo> properties = type.GetProperties().ToList();
      foreach (PropertyInfo info in properties)
      {
          propertyNames.Add(info.Name);
      }
      return propertyNames;
    }

    public static void SetValue(this PropertyInfo propertyInfo, object obj, string value)
    {
      if (propertyInfo.PropertyType == typeof(string))
      {
        propertyInfo.SetValue(obj, value, null);
      }
      else if (propertyInfo.PropertyType == typeof(bool))
      {
        propertyInfo.SetValue(obj, Convert.ToBoolean(value), null);
      }
      else if (propertyInfo.PropertyType == typeof(int))
      {
        propertyInfo.SetValue(obj, Convert.ToInt32(value), null);
      }
      else if (propertyInfo.PropertyType == typeof(DateTime))
      {
        propertyInfo.SetValue(obj, Convert.ToDateTime(value), null);
      }
    }

    public static bool ContainsType(this ObservableCollection<ViewModelBase> collection, Type t)
    {
      return ((from ViewModelBase vm in collection where vm.GetType() == t select t).FirstOrDefault() != null);
    }

  }

  internal static class Utilities
  {
    public static TEnum ParseFromXml<TEnum>(string value)
    {
      Type typeParameterType = typeof(TEnum);
      if (typeParameterType.IsEnum)
      {

        var fis = typeParameterType.GetFields();
        foreach (FieldInfo fi in fis)
        {
          var enumattrib = fi.GetCustomAttributes(typeof(XmlEnumAttribute), false).FirstOrDefault() as XmlEnumAttribute;
          if (enumattrib != null)
          {
            if (String.Compare(enumattrib.Name, value) == 0)
            {
              return (TEnum)Enum.Parse(typeParameterType, fi.Name);
            }
          }
        }

        throw new ApplicationException();
      }
      else
      {
        throw new ApplicationException();
      }

    }

  }
}
