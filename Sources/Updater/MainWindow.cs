using SwiftXP.SPT.TheModfather.Updater.Helpers;
using SwiftXP.SPT.TheModfather.Updater.Services.Interfaces;

namespace SwiftXP.SPT.TheModfather.Updater;

public partial class MainWindow : Form
{
    private readonly IUpdaterService _updaterService;

    public MainWindow(IUpdaterService updaterService)
    {
        _updaterService = updaterService;

        InitializeComponent();
    }

    private async void MainWindow_Load(object sender, EventArgs e)
    {
        bool updated = await _updaterService.Update();
        string updateStatus = "Update completed.";

        if (!updated)
        {
            updateStatus = "Update failed. Please check the log file.";

            ProgressBarColorHelper.SetProgressBarState(ProgressBar, 2);
            ProgressBar.Style = ProgressBarStyle.Blocks;
            ProgressBar.Value = ProgressBar.Maximum;
        }
        else
        {
            ProgressBar.Style = ProgressBarStyle.Blocks;
            ProgressBar.Value = ProgressBar.Maximum;
        }

        int countdown = 0;
        while (countdown < 3)
        {
            UpdateStatusText($"{updateStatus} Closing window in {3 - countdown++} second(s)...");
            await Task.Delay(1000);
        }

        this.Close();
    }

    private void UpdateStatusText(string text)
    {
        StatusText.Text = text;
        StatusText.Left = (this.ClientSize.Width - StatusText.Width) / 2;
    }
}
