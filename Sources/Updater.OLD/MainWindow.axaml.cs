using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using SwiftXP.SPT.TheModfather.Updater.Services;

namespace SwiftXP.SPT.TheModfather.Updater;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        await FinishUpdate();
    }

    private async Task FinishUpdate()
    {
        bool updateFinished = await Task.Run(async () =>
        {
            FinishUpdateService finishUpdateService = new();

            return await finishUpdateService.FinishAsync();
        });

        string updateStatus = "Update finished.";
        
        if(!updateFinished)
            updateStatus = "Update failed. Please check log file.";

        int countdown = 0;
        while(countdown < 3)
        {
            UpdateStatus($"{updateStatus} Closing window in {3 - countdown++} second(s)...");

            await Task.Delay(1000);
        }

        this.Close();
    }
    
    private void UpdateStatus(string newMessage)
    {
        StatusText.Text = newMessage;
    }
}