using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;

namespace PitMaker
{
  class CreateTypeCommand : INotifyPropertyChanged
  {

    public CreateTypeCommand(Type type, ICommand command)
    {
      this.DisplayName = type.Name;
      this.TypeName = type.FullName;
      this.Command = command;
    }

    public CreateTypeCommand(string displayName, Type type, ICommand command)
    {
      this.DisplayName = displayName;
      this.TypeName = type.FullName;
      this.Command = command;
    }

    #region DisplayName Property

    private string displayNameField;

    public string DisplayName
    {
      get
      {
        return this.displayNameField;
      }
      set
      {
        if (this.displayNameField != value)
        {
          this.displayNameField = value;
          RaisePropertyChanged("DisplayName");
        }
      }
    }

    #endregion

    #region TypeName Property

    private string typeNameField;

    public string TypeName
    {
      get
      {
        return this.typeNameField;
      }
      set
      {
        if (this.typeNameField != value)
        {
          this.typeNameField = value;
          RaisePropertyChanged("TypeName");
        }
      }
    }

    #endregion

    #region Command Property

    private ICommand commandField;

    public ICommand Command
    {
      get
      {
        return this.commandField;
      }
      set
      {
        if (this.commandField != value)
        {
          this.commandField = value;
          RaisePropertyChanged("Command");
        }
      }
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;

    internal void RaisePropertyChanged(string propertyName)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
      }
    }

    internal void RaisePropertyChanged(string propertyName, object oldValue, object newValue)
    {
      if (PropertyChanged != null)
      {
        PropertyChanged(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
      }
    }

    #endregion

  }
}
