using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PitMaker.Models
{
  [System.SerializableAttribute()]
  [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://phed.org/2012/Peach")]
  [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://phed.org/2012/Peach", IsNullable = false)]
  public class Result : Node
  {
  }
}
