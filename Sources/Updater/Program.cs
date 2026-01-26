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
            ApplicationConfiguration.Initialize();

            ILogService logService = new LogService();
            IProcessWatcher processWatcher = new ProcessWatcher(logService);
            IDeleteService deleteService = new DeleteService(logService);
            IMoverService moverService = new MoverService(logService);

            IUpdaterService updaterService = new UpdaterService(logService, processWatcher, deleteService, moverService);

            if(CommandLineParameterService.IsSilent())
            {
                await updaterService.Update();
            }
            else
            {
                Application.Run(new MainWindow(updaterService));
            }
        }
    }
}