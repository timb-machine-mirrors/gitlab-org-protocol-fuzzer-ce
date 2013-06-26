using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using T = Telerik.Windows.Controls;
using TDragDrop = Telerik.Windows.Controls.DragDrop;
using PitMaker.ViewModels;
using PitMaker.Models;
using Microsoft.Win32;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.ComponentModel;
using System.Reflection;


namespace PitMaker
{
	/// <summary>
	/// Interaction logic for PitMakerControl.xaml
	/// </summary>
	public partial class PitMakerControl : UserControl
	{
		public PitMakerControl()
		{
			this.InitializeComponent();
      this.Loaded += new RoutedEventHandler(PitMakerControl_Loaded);
      treeView.AddHandler(TDragDrop.RadDragAndDropManager.DropQueryEvent,
        new EventHandler<TDragDrop.DragDropQueryEventArgs>(OnDropInsideTreeViewDropQuery), true);

      treeView.AddHandler(TDragDrop.RadDragAndDropManager.DropInfoEvent,
        new EventHandler<TDragDrop.DragDropEventArgs>(OnDropCompleted), true);

      propertyGrid.AutoGeneratingPropertyDefinition += propertyGrid_AutoGeneratingPropertyDefinition;

      //MainViewModel vm = new MainViewModel();
      //PeachViewModel pvm = new PeachViewModel(new PeachPitDocument(), vm);
      //pvm.MessageRaised += pvm_MessageRaised;
      //vm.Add(pvm);

      //this.DataContext = vm;

		}

    private bool isDirtyField = false;
    
    public bool IsDirty
    {
      get
      {
        if (isDirtyField)
        {
          return true;
        }
        else
        {
          if (this.DataContext != null)
          {
            return ((MainViewModel)this.DataContext).IsDirty;
          }
          else
          {
            return false;
          }
        }
      }
      set
      {
        isDirtyField = value;
      }
    }

    void propertyGrid_AutoGeneratingPropertyDefinition(object sender, T.Data.PropertyGrid.AutoGeneratingPropertyDefinitionEventArgs e)
    {
      #region fix dupes

      foreach (T.Data.PropertyGrid.PropertyDefinition pd in propertyGrid.PropertyDefinitions)
      {
        if (pd.DisplayName == e.PropertyDefinition.DisplayName)
        {
          e.Cancel = true;
          return;
        }
      }
      
      #endregion

      #region process attributes
      ViewModelBase vm = propertyGrid.Item as ViewModelBase;
      Node model = vm.Model;
      PropertyDescriptor propdesc = null;
      bool foundproperty = false;

      if (model is NodeWithParameters)
      {
        Peach.Core.ParameterAttribute pa = ((NodeWithParameters)model).Parameters.GetKey(e.PropertyDefinition.DisplayName);
        if (pa != null)
        {
          foundproperty = true;
          e.PropertyDefinition.Description = pa.description;
          if (pa.required)
            e.PropertyDefinition.GroupName = Categories.Required;
          else
            e.PropertyDefinition.GroupName = Categories.Optional;
        }
      }
      if (!foundproperty)
      {
        try
        {
          propdesc = TypeDescriptor.GetProperties(model)[e.PropertyDefinition.DisplayName];
        }
        finally { }

        if (propdesc != null)
        {
          #region category
          CategoryAttribute category = propdesc.Attributes[typeof(CategoryAttribute)] as CategoryAttribute;
          if (category != null)
          {
            if (category.Category == "Misc")
            {
              e.PropertyDefinition.GroupName = Categories.Required;
            }
            else
            {
              e.PropertyDefinition.GroupName = category.Category;
            }
          }
          #endregion
        }
      }
      #endregion

    }

    void PitMakerControl_Loaded(object sender, RoutedEventArgs e)
    {

    }

    void OnDropCompleted(object sender, TDragDrop.DragDropEventArgs e)
    {
      if (e.Options.Status == TDragDrop.DragStatus.DropComplete)
      {
        var source = e.OriginalSource as T.RadTreeViewItem;
        ((ViewModelBase)source.DataContext).Parent.ReorderItems();
      }
    }

    private void OnDropInsideTreeViewDropQuery(object sender, TDragDrop.DragDropQueryEventArgs e)
    {
      var source = e.Options.Source as T.RadTreeViewItem;
      var destination = e.Options.Destination as T.RadTreeViewItem;
      if (destination != null)
      {
				if (source == destination)
				{
					e.QueryResult = false;
				}
        else if (
          (source.DataContext is TestIncludeExcludeViewModel)
          && (destination.DataContext is TestIncludeExcludeViewModel)
          && (destination.DropPosition != T.DropPosition.Inside)
          )
        {
          e.QueryResult = true;
          return;
        }
        else if (
          ((source.DataContext is DataElementViewModel) || (source.DataContext is DataElementContainerViewModel))
          && ((destination.DataContext is DataElementViewModel) || (destination.DataContext is DataElementViewModel))
          && (destination.DropPosition != T.DropPosition.Inside)
          )
        {
          e.QueryResult = true;
          return;
        }
        else if (
          ((source.DataContext is ActionViewModel))
          && ((destination.DataContext is ActionViewModel))
          && (destination.DropPosition != T.DropPosition.Inside)
          )
        {
          e.QueryResult = true;
          return;
        }
      }

      e.QueryResult = false;
    }

    //BackgroundWorker worker = null;
    public void LoadPitFile(Stream stream)
    {
      this.ForceCursor = true;
      this.Cursor = Cursors.Arrow;
      this.IsEnabled = false;

      try
      {
        this.DataContext = LoadPitXml(stream);
      }
      finally
      {
        this.Cursor = Cursors.Arrow;
        this.ForceCursor = false;
        this.IsEnabled = true;
      }
    }

    public void LoadPitFile(string fileName)
    {
      LoadPitFile(new FileStream(fileName, FileMode.Open, FileAccess.Read));
    }

    void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
      if (e.Cancelled)
      {

      }
      else if (e.Error != null)
      {
        throw e.Error;
      }
      else
      {
        this.DataContext = (MainViewModel)e.Result;
      }
      this.Cursor = Cursors.Arrow;
      this.ForceCursor = false;
      this.IsEnabled = true;
    }

    void worker_DoWork(object sender, DoWorkEventArgs e)
    {
      e.Result = LoadPitXml(new FileStream((string)e.Argument, FileMode.Open));
    }

    public void SavePitFile(string fileName, bool overwriteUi = true)
    {
			//if(File.Exists(fileName))
			//{
			//  if (overwriteUi)
			//  {
			//    if (MessageBox.Show("File \"" + fileName + "\" exists. Overwrite?", "Fuzz Factory", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
			//      return;
			//  }
			//}

		  using (FileStream stream = new FileStream(fileName, FileMode.Create))
      using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8))
      {
        writer.Write(GetPitXml());
        writer.Flush();
      }
      this.IsDirty = false;

      this.IsEnabled = true;
      this.Cursor = Cursors.Arrow;

    }

    private MainViewModel LoadPitXml(Stream stream)
    {
      XmlReader reader = null;
      MainViewModel mvm = null;
      try
      {

        XmlSerializer serializer = new XmlSerializer(typeof(PeachPitDocument));
        
        reader = new XmlTextReader(stream);
        PeachPitDocument model = (PeachPitDocument)serializer.Deserialize(reader);

        mvm = new MainViewModel();
        PeachViewModel pvm = new PeachViewModel(model, mvm);
        pvm.MessageRaised += pvm_MessageRaised;
        mvm.Add(pvm);

      }
      catch (Exception ex)
      {
        ex.WriteAll();
        ApplicationException pex = new ApplicationException("Pit file is invalid\r\n" + ex.Message, ex.InnerException);
        throw pex;
      }
      finally
      {
        if (reader != null)
          reader.Close();

        stream.Close();
      }
      return mvm;
    }

    void pvm_MessageRaised(object sender, PeachViewModel.MessageRaisedEventArgs e)
    {
      MessageBox.Show(e.Message);
    }

    public string GetPitXml()
    {
      MainViewModel mvm = (MainViewModel)this.DataContext;

      using (MemoryStream stream = new MemoryStream())
      {
        mvm.Save(stream, false);

        stream.Seek(0, SeekOrigin.Begin);

        StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        string xml = reader.ReadToEnd();
        return xml;
      }
      
    }
	}
}