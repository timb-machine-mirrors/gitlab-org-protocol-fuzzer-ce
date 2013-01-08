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


      txtMessage.Text = WriteAll(ex, "", message);

    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
      //Application.Current.Shutdown();
    }

    public string WriteAll(Exception ex, string prefix, StringBuilder concat)
    {
      concat.AppendLine(prefix + ex.Message);


//#if DEBUG
//      concat.AppendLine(ex.StackTrace);
//#endif

      if (ex.InnerException != null)
      {
        concat.Append(WriteAll(ex.InnerException, prefix + "--", concat));
      }
      return concat.ToString();
    }
  }
}
