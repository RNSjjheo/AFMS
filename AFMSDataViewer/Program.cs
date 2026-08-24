using Krypton.Toolkit;

namespace AFMSDataViewer
{
    internal static class Program
    {
        // 프로그램이 실행되는 동안 유지되도록 정적 필드로 선언
        private static readonly KryptonManager _kryptonManager = new();
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            //_kryptonManager.GlobalPaletteMode = PaletteMode.Microsoft365BlackDarkMode;

            Application.Run(new FormMain());
        }
    }
}