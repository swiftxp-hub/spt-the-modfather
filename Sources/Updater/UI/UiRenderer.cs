using Spectre.Console;
using Spectre.Console.Rendering;

namespace SwiftXP.SPT.TheModfather.Updater.UI;

public static class UiRenderer
{
    public static IRenderable CreateCenteredPanel(string headerText, string statusText, int percentage)
    {
        bool supportsUnicode = AnsiConsole.Profile.Capabilities.Unicode;

        char fillChar = supportsUnicode ? '█' : '#';
        char emptyChar = supportsUnicode ? '░' : '-';

        const int width = 30;
        int filled = (int)(percentage / 100.0 * width);
        int empty = width - filled;

        string progressBar =
            $"[green]{new string(fillChar, filled)}[/]" +
            $"[grey]{new string(emptyChar, empty)}[/]";

        Grid content = new Grid()
                .AddColumn(new GridColumn().NoWrap().Centered())
                .AddRow(statusText)
                .AddRow(" ")
                .AddRow(progressBar)
                .AddRow($"[grey]{percentage}%[/]");

        BoxBorder border = supportsUnicode ? BoxBorder.Rounded : BoxBorder.Ascii;

        Panel panel = new Panel(content)
                .Header(headerText)
                .Border(border)
                .BorderColor(Color.Blue)
                .Padding(2, 1);

        return Align.Center(panel, VerticalAlignment.Middle);
    }
}