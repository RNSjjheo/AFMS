using System;
using System.Collections.Generic;
using System.Text;

namespace AFMSDataViewer
{
    public class BorderlessToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            // ToolStrip 기본 외곽선을 그리지 않습니다.
        }
    }
}
