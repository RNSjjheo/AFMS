using RnsLibrary;

namespace AFMSExtraMonitor.Source.Main
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            RnsLog.Init(Environment.UserInteractive, "AFMSExtraLogger", 100, 0);
            RnsLog.Start();
            RnsLog.AppenderInfo();
            RnsLog.ShowVersion();

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApplicationContext());
        }
    }
}