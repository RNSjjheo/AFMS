using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace AFMSDll
{
    public class HeaderlessTabControl : TabControl
    {
        private const int TcmAdjustRect = 0x1328;

        protected override void WndProc(ref Message message)
        {
            bool designMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;

            if (!designMode && message.Msg == TcmAdjustRect)
            {
                message.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref message);
        }
    }
}
