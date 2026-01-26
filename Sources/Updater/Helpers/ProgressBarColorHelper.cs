using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SwiftXP.SPT.TheModfather.Updater.Helpers
{
    public static class ProgressBarColorHelper
    {
        /// <summary>
        /// State: 1 = normal (green); 2 = error (red); 3 = warning (yellow)
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = false)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr w, IntPtr l);
        public static void SetProgressBarState(this ProgressBar progressBar, int state)
        {
            try { SendMessage(progressBar.Handle, 1040, (IntPtr)state, IntPtr.Zero); } catch { }
        }
    }
}