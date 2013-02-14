using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Win32;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Input;
using Microsoft.Practices.Prism.Commands;
using System.Xml;

namespace PitMaker.ViewModels
{
  class MainViewModel : ViewModelBase
  {
    public MainViewModel()
      :base(null, null)
    {
      this.SaveCommand = new DelegateCommand(ExecuteSave, () => { return true; });
    }

    public new bool IsDirty
    {
      get
      {
        foreach (var vm in this.Children)
        {
          if (vm.IsDirty)
            return true;
        }
        return false;
      }
    }

    internal void Add(PeachViewModel pvm)
    {
      if (this.Children == null)
        this.Children = new ObservableCollection<ViewModelBase>();

      if(pvm.Parent == null)
        pvm.Parent = this;

      this.Children.Add(pvm);

    }

    #region Save Command

    [Browsable(false)]
    public ICommand SaveCommand { get; set; }

    private void ExecuteSave()
    {
    }

    private bool CanSave()
    {
      return IsDirty;
    }


    public void Save(Stream stream, bool close = true)
    {
      PeachViewModel viewModel = (PeachViewModel)this.Children[0];
      Models.PeachPitDocument model = (Models.PeachPitDocument)viewModel.Model;
      //model.Version += 1;
      model.SortItems();

      XmlWriterSettings settings = new XmlWriterSettings();
      settings.ConformanceLevel = ConformanceLevel.Auto;
      settings.Indent = true;
      settings.OmitXmlDeclaration = true;
      XmlWriter writer = XmlWriter.Create(stream, settings);
      try
      {
        XmlSerializer serializer = new XmlSerializer(typeof(Models.PeachPitDocument));
        serializer.Serialize(writer, model);
        writer.Flush();
      }
      catch (Exception ex)
      {
        ex.WriteAll();
        throw ex;
      }
      finally
      {
        writer.Close();
        if (close)
        {
          stream.Close();
        }
      }
      viewModel.IsDirty = false;
    }

    #endregion
  }
}
