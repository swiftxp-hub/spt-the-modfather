using SwiftXP.SPT.Common.Runtime;
using SwiftXP.SPT.TheModfather.Updater.Controls;

namespace SwiftXP.SPT.TheModfather.Updater
{
    partial class MainWindow
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
            this.HeaderText = new WineCompatibleLabel();
            this.StatusText = new WineCompatibleLabel();
            this.ProgressBar = new WineCompatibleProgressBar();
            this.SuspendLayout();
            // 
            // HeaderText
            // 
            this.HeaderText.AutoSize = true;
            this.HeaderText.Font = new Font("Arial", 18F, FontStyle.Bold, GraphicsUnit.Point, 0);
            this.HeaderText.ForeColor = Color.White;
            this.HeaderText.Location = new Point(97, 70);
            this.HeaderText.Name = "HeaderText";
            this.HeaderText.Size = new Size(307, 29);
            this.HeaderText.TabIndex = 0;
            this.HeaderText.Text = $"THE MODFATHER ({AppMetadata.Version})";
            // 
            // StatusText
            // 
            this.StatusText.AutoSize = true;
            this.StatusText.Font = new Font("Arial", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.StatusText.ForeColor = Color.LightGray;
            this.StatusText.Location = new Point(108, 116);
            this.StatusText.Name = "StatusText";
            this.StatusText.Size = new Size(284, 18);
            this.StatusText.TabIndex = 1;
            this.StatusText.Text = "Please wait. Finishing update process...";
            this.StatusText.TextAlign = ContentAlignment.TopCenter;
            // 
            // ProgressBar
            // 
            this.ProgressBar.Location = new Point(75, 160);
            this.ProgressBar.Name = "ProgressBar";
            this.ProgressBar.Size = new Size(350, 15);
            this.ProgressBar.Style = ProgressBarStyle.Marquee;
            this.ProgressBar.TabIndex = 2;
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new SizeF(6F, 15F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(51, 51, 51);
            this.ClientSize = new Size(500, 250);
            this.Controls.Add(this.ProgressBar);
            this.Controls.Add(this.StatusText);
            this.Controls.Add(this.HeaderText);
            this.Font = new Font("Arial", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.FormBorderStyle = FormBorderStyle.None;
            this.Name = "MainWindow";
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.TopMost = true;
            this.Load += this.MainWindow_Load;
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private WineCompatibleLabel HeaderText;
        private WineCompatibleLabel StatusText;
        private WineCompatibleProgressBar ProgressBar;
    }
}
