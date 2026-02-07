using Spectre.Console;
using Spectre.Console.Rendering;

namespace SwiftXP.SPT.TheModfather.Updater.UI;

public static class UiRenderer
{
    public static IRenderable CreateCenteredPanel(string statusText, int percentage, string headerText)
    {
        const int width = 30;
        int filled = (int)((percentage / 100.0) * width);
        int empty = width - filled;

        string progressBar =
            $"[green]{new string('█', filled)}[/]" +
            $"[grey]{new string('░', empty)}[/]";

        Grid content = new Grid()
                .AddColumn(new GridColumn().NoWrap().Centered())
                .AddRow(statusText)
                .AddRow(" ")
                .AddRow(progressBar)
                .AddRow($"[grey]{percentage}%[/]");

        Panel panel = new Panel(content)
                .Header(headerText)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(2, 1);

        return Align.Center(panel, VerticalAlignment.Middle);
    }
}