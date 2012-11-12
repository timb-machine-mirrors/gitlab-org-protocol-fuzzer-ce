using System;
using System.Text;
using System.Windows;

namespace PeachFuzzFactory
{
  /// <summary>
  /// Interaction logic for UnhandledException.xaml
  /// </summary>
  public partial class UnhandledException : Window
  {
    public UnhandledException(Exception ex)
    {
      InitializeComponent();
      StringBuilder message = new StringBuilder();

      message.Append(ex.Message);
      if (ex.InnerException != null)
      {
        message.Append(Environment.NewLine);
        message.Append(Environment.NewLine);
        message.Append(ex.InnerException.Message);
      }

      txtMessage.Text = message.ToString();
    }
  }
}
