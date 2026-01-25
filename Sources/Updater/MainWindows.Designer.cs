namespace SwiftXP.SPT.TheModfather.Updater;

partial class MainWindows
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.HeaderText = new Label();
        this.StatusText = new Label();
        this.ProgressBar = new ProgressBar();
        this.SuspendLayout();
        // 
        // HeaderText
        // 
        this.HeaderText.AutoSize = true;
        this.HeaderText.Font = new Font("Arial", 22F, FontStyle.Bold);
        this.HeaderText.ForeColor = Color.White;
        this.HeaderText.Location = new Point(76, 41);
        this.HeaderText.Name = "HeaderText";
        this.HeaderText.Size = new Size(333, 35);
        this.HeaderText.TabIndex = 0;
        this.HeaderText.Text = "The Modfather (v0.1.1)";
        this.HeaderText.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // StatusText
        // 
        this.StatusText.AutoSize = true;
        this.StatusText.Font = new Font("Arial", 11F);
        this.StatusText.ForeColor = Color.LightGray;
        this.StatusText.Location = new Point(109, 97);
        this.StatusText.Name = "StatusText";
        this.StatusText.Size = new Size(266, 17);
        this.StatusText.TabIndex = 1;
        this.StatusText.Text = "Please wait. Finishing update process...";
        this.StatusText.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // ProgressBar
        // 
        this.ProgressBar.Location = new Point(67, 139);
        this.ProgressBar.MarqueeAnimationSpeed = 60;
        this.ProgressBar.Name = "ProgressBar";
        this.ProgressBar.Size = new Size(350, 15);
        this.ProgressBar.Style = ProgressBarStyle.Marquee;
        this.ProgressBar.TabIndex = 2;
        // 
        // MainWindows
        // 
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.BackColor = Color.FromArgb(51, 51, 51);
        this.ClientSize = new Size(484, 211);
        this.ControlBox = false;
        this.Controls.Add(this.ProgressBar);
        this.Controls.Add(this.StatusText);
        this.Controls.Add(this.HeaderText);
        this.FormBorderStyle = FormBorderStyle.None;
        this.Name = "MainWindows";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Form1";
        this.Load += this.MainWindows_Load;
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private Label HeaderText;
    private Label StatusText;
    private ProgressBar ProgressBar;
}
