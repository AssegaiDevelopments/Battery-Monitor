namespace BatteryMonitor;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
        Application.ThreadException += (s, e) => MessageBox.Show(e.Exception.ToString(), "Thread Exception");

        AppDomain.CurrentDomain.UnhandledException += (s, e) => MessageBox.Show(e.ExceptionObject.ToString(), "Unhandled Exception");
    }
}