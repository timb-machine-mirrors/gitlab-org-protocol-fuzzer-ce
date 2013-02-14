using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace PitMaker
{
  public class PropertyChangedEventArgsEx : PropertyChangedEventArgs
  {
    public PropertyChangedEventArgsEx(string propertyName, object oldValue, object newValue)
      : base(propertyName)
    {
      this.OldValue = oldValue;
      this.NewValue = newValue;
    }

    public object OldValue { get; set; }

    public object NewValue { get; set; }
  }

}
