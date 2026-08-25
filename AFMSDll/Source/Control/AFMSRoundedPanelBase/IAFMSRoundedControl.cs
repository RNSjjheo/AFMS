using System.Drawing;

namespace AFMSDll
{
    public interface IAFMSRoundedControl
    {
        int BorderRadius { get; set; }
        Color BorderColor { get; set; }
        float BorderThickness { get; set; }
    }
}
