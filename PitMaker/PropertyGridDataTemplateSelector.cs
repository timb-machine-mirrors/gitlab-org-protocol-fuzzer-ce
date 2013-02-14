using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using Telerik.Windows.Controls.Data.PropertyGrid;
using System.ComponentModel;
using System.Diagnostics;

namespace PitMaker
{
  public class PropertyGridDataTemplateSelector : DataTemplateSelector
  {
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
      if (item as PropertyDefinition != null)
      {
        PropertyDefinition propdef = (PropertyDefinition)item;
        PropertyDescriptor descriptor = (PropertyDescriptor)propdef.SourceProperty.Descriptor;
        ViewModels.ViewModelBase vm = (ViewModels.ViewModelBase)((PropertyGridField)container).ParentPropertyGrid.Item;
        string viewModelTypeName = vm.GetType().Name;
        Models.Node model = vm.Model;
        //if(propdef.SourceProperty.Descriptor is ReflectionPropertyDescriptor)
        switch (propdef.SourceProperty.Name)
        {
          case "InitialState":
            return StateSelectorDataTemplate;
          case "StateReference":
            return ChangeStateSelectorDataTemplate;
          case "StateModelReference":
            return StateModelSelectorDataTemplate;
          case "DataModelReference":
            return DataModelSelectorDataTemplate;
          case "AgentReference":
            return AgentSelectorTemplate;
          case "FileName":
            return FileSelectorDataTemplate;
          case "ref":
            List<string> names = new List<string>() { "DataElementContainerViewModel", "DataElementViewModel" };
            if(names.Contains(viewModelTypeName))
              return DataElementSelectorTemplate;
            else
              return null;
        }
      }
      return null;
    }

    public DataTemplate StateSelectorDataTemplate { get; set; }

    public DataTemplate ChangeStateSelectorDataTemplate { get; set; }

    public DataTemplate DataModelSelectorDataTemplate { get; set; }

    public DataTemplate StateModelSelectorDataTemplate { get; set; }

    public DataTemplate FileSelectorDataTemplate { get; set; }

    public DataTemplate DataElementSelectorTemplate { get; set; }

    public DataTemplate AgentSelectorTemplate { get; set; }
  }

}
