using Avalonia;
using SwiftXP.SPT.TheModfather.Updater.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SwiftXP.SPT.TheModfather.Updater;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        if (!File.Exists("EscapeFromTarkov.exe"))
        {
            SimpleLogService.Error("EscapeFromTarkov.exe could not be found. Make sure you are running the updater from your SPT folder. Exiting...");

            return;
        }

        SimpleLogService.StartNewFile();
        SimpleLogService.Write("Starting up...");

        if(CommandLineParameterService.IsSilent())
        {
            SimpleLogService.Write("Silent finalization of update(s)...");

            FinishUpdateService finishUpdateService = new();

            await finishUpdateService.FinishAsync();
        }
        else
        {
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
