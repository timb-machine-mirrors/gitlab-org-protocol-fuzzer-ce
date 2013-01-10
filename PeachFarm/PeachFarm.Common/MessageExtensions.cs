using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Xml.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Builders;
using System.Security.Cryptography;

namespace PeachFarm.Common.Messages
{
   
  public partial class StartPeachRequest
  {
    #region pit file support
    /*  
    public static StartPeachRequest CreateStartPeachRequest(string pitFileName)
    {
      StreamReader reader = null;
      string pitFileText;
      try
      {
        reader = new StreamReader(pitFileName);
        pitFileText = reader.ReadToEnd();
      }
      catch
      {
        throw;
      }
      finally
      {
        if(reader != null)
        {
          reader.Close();
        }
      }

      StartPeachRequest request = new StartPeachRequest();
      request.PitXml = pitFileText;

      return request;
    }
    //*/
    #endregion
  }

  #region Peach 2
  /*
  public partial class LogFile
  {
    //[BsonIgnore]
    //[XmlElement(Order = 1)]
    //public byte[] Data { get; set; }

    [BsonIgnore]
    [XmlElement(Order = 1)]
    public string LocalFileName { get; set; }

    [XmlIgnore]
    public BsonObjectId FileReference { get; set; }
  }
  //*/
  #endregion
}
