using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using SMS_API_Demo;

namespace WindowsFormsApplication1 {
  static class Program {
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
      Application.EnableVisualStyles();
      Application.SetCompatibleTextRenderingDefault(false);

      // Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

      // Declare local variable for the main form and create a new instance.  Must not be performed before SetUnhandledExceptionMode().
      SMS_API_DemoForm form;
      form = new SMS_API_DemoForm(args);

      // Add event handler for handling UI thread exceptions.
      Application.ThreadException += new ThreadExceptionEventHandler(form.HandleUIException);

      // Add event handler for handling non-UI thread exceptions to the event.
      AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(form.HandleNonUIException);

      Application.Run(form);
    }
  }
}
