using System;
using System.Windows.Forms;

namespace SwiftXP.SPT.TheModfather.Updater.Controls;

public class WineCompatibleProgressBar : ProgressBar
{
    protected override void WndProc(ref Message m)
    {
        const int WM_GETOBJECT = 0x003D;

        if (m.Msg == WM_GETOBJECT)
        {
            m.Result = IntPtr.Zero;

            return;
        }

        base.WndProc(ref m);
    }
}