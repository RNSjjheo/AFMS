using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDll
{
    public class AMFSHiddenTabControl : TabControl
    {
        public AMFSHiddenTabControl()
        {
            Appearance = TabAppearance.Buttons;
            SizeMode = TabSizeMode.Fixed;
            ItemSize = new Size(0, 1);
            Multiline = true;
            BackColor = Color.White;
        }

        public override Rectangle DisplayRectangle => ClientRectangle;
    }
}
