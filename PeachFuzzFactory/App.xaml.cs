using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace PeachFuzzFactory
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
    public App()
    {
      this.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
    }

    void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
      if ((e.Exception is ApplicationException) || (e.Exception is Peach.Core.PeachException))
      {
        MessageBox.Show(e.Exception.Message);
      }
      else
      {
        UnhandledException u = new UnhandledException(e.Exception);
        u.Show();
      }
      e.Handled = true;

    }
	}
}
