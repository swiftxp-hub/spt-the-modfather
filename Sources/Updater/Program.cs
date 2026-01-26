using SwiftXP.SPT.TheModfather.Updater.Services;
using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

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
            LogService logService = new();
            ProcessWatcher processWatcher = new(logService);
            DeleteService deleteService = new(logService);
            MoverService moverService = new(logService);

            UpdaterService updaterService = new(logService, processWatcher, deleteService, moverService);

            if (CommandLineParameterService.IsSilent())
            {
                logService.WriteMessage("Silent mode activated");

                await updaterService.UpdateModsAsync();
            }
            else
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainWindow(updaterService));
            }
        }
    }
}