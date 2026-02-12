using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using Spectre.Console.Rendering;
using SwiftXP.SPT.TheModfather.Updater.Diagnostics;
using SwiftXP.SPT.TheModfather.Updater.Environment;
using SwiftXP.SPT.TheModfather.Updater.Logging;
using SwiftXP.SPT.TheModfather.Updater.Services;
using SwiftXP.SPT.TheModfather.Updater.UI;

namespace SwiftXP.SPT.TheModfather.Updater;

public static class Program
{
    private static readonly CancellationTokenSource s_cancellationTokenSource = new();

    private const string HeaderText = "The Modfather Updater";

    static async Task Main(string[] args)
    {

        bool isWine = System.Environment.GetEnvironmentVariable("WINEUSERNAME") != null
               || System.Environment.GetEnvironmentVariable("WINELOADER") != null
               || System.Environment.GetEnvironmentVariable("WINEPREFIX") != null;

        if (args.Any(a => a.Equals("--ascii", StringComparison.OrdinalIgnoreCase)))
            isWine = true;

        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (isWine)
        {
            AnsiConsole.Profile.Capabilities.Unicode = false;
            AnsiConsole.Profile.Capabilities.Ansi = true;
        }

        SubscribeToCancelKeyPress();

        SimpleLogger simpleLogger = new(AppContext.BaseDirectory);
        CommandLineArgsReader commandLineArgsReader = new();
        ProcessService processService = new();

        EFTProcessWatcher eftProcessWatcher = new(simpleLogger, commandLineArgsReader, processService);

        UpdateManager updateManager = new(simpleLogger, eftProcessWatcher);

        try
        {
            await simpleLogger.StartNewFile(s_cancellationTokenSource.Token);

            if (commandLineArgsReader.IsSilent())
            {
                await StartSilentUpdate(updateManager);
            }
            else
            {
                if (OperatingSystem.IsWindows())
                    WindowHelper.CenterAndTopMost();

                await StartUiUpdate(updateManager);
            }
        }
        catch (OperationCanceledException)
        {
            if (!commandLineArgsReader.IsSilent())
                AnsiConsole.MarkupLine("\n[yellow]Update aborted. Warning: Partial updates may have occurred.[/]");
        }
        catch (Exception exception)
        {
            if (!commandLineArgsReader.IsSilent())
                AnsiConsole.MarkupLine($"\n[red]An error occurred: {exception.Message}[/]");

            await simpleLogger.WriteErrorAsync("An error occured.", exception);

            System.Environment.Exit(1);
        }
        finally
        {
            s_cancellationTokenSource.Dispose();
        }
    }

    private static async Task StartSilentUpdate(UpdateManager updateManager)
    {
        Progress<int> silentProgress = new(_ => { });
        await updateManager.ProcessUpdatesAsync(silentProgress, s_cancellationTokenSource.Token);
    }

    private static async Task StartUiUpdate(UpdateManager updateManager)
    {
        IRenderable layout = UiRenderer.CreateCenteredPanel(HeaderText, "Initialising...", 0);

        await AnsiConsole.Live(layout)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Ellipsis)
            .Cropping(VerticalOverflowCropping.Top)
            .StartAsync(async ctx =>
            {
                Progress<int> progress = new(percentage =>
                {
                    IRenderable newLayout = UiRenderer.CreateCenteredPanel(
                        HeaderText,
                        $"Processing updates... ({percentage}%)",
                        percentage);
                    ctx.UpdateTarget(newLayout);
                });

                await updateManager.ProcessUpdatesAsync(progress, s_cancellationTokenSource.Token);

                ctx.UpdateTarget(UiRenderer.CreateCenteredPanel(HeaderText, "[green]Update completed! Closing...[/]", 100));

                await Task.Delay(1500, s_cancellationTokenSource.Token);
            });
    }

    private static void SubscribeToCancelKeyPress()
    {
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            s_cancellationTokenSource.Cancel();
        };
    }
}