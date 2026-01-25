using System.Runtime.InteropServices;
using SwiftXP.SPT.TheModfather.Updater.Helpers;
using SwiftXP.SPT.TheModfather.Updater.Services;

namespace SwiftXP.SPT.TheModfather.Updater;

public partial class MainWindows : Form
{
    public MainWindows()
    {
        InitializeComponent();
    }

    private async void MainWindows_Load(object sender, EventArgs e)
    {
        await FinishUpdate();
        
        HeaderText.AutoSize = true;
        StatusText.AutoSize = true;

        CenterHeaderText();
        CenterStatusText();
    }

    private async Task FinishUpdate()
    {
        bool updateFinished = await Task.Run(async () =>
        {
            FinishUpdateService finishUpdateService = new();

            return await finishUpdateService.FinishAsync();
        });

        string updateStatus = "Update finished.";
        ProgressBar.Value = ProgressBar.Maximum;
        ProgressBar.Style = ProgressBarStyle.Blocks;

        if (!updateFinished)
        {
            updateStatus = "Update failed. Please check log file.";
            ProgressBarColorHelper.SetProgressBarState(ProgressBar, 2);
        }

        int countdown = 0;
        while (countdown < 3)
        {
            UpdateStatus($"{updateStatus} Closing window in {3 - countdown++} second(s)...");

            await Task.Delay(1000);
        }

        this.Close();
    }

    private void UpdateStatus(string newMessage)
    {
        StatusText.Text = newMessage;
        
        CenterStatusText();
    }

    private void CenterHeaderText()
    {
        int x = (this.ClientSize.Width - HeaderText.Width) / 2;
        HeaderText.Location = new Point(Math.Max(0, x), HeaderText.Location.Y);
    }

    private void CenterStatusText()
    {
        int x = (this.ClientSize.Width - StatusText.Width) / 2;
        StatusText.Location = new Point(Math.Max(0, x), StatusText.Location.Y);
    }
}
