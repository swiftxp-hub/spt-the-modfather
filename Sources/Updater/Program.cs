using SwiftXP.SPT.TheModfather.Updater.Logging;
using SwiftXP.SPT.TheModfather.Updater.Utilities;

namespace SwiftXP.SPT.TheModfather.Updater
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            if (CommandLineParameterUtility.IsSilent())
            {
                StaticLog.WriteMessage("Silent mode activated");

                await UpdaterUtility.UpdateModsAsync();
            }
            else
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainWindow());
            }
        }
    }
}