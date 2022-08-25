using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RainbowBraces
{
    internal partial class OptionsProvider
    {
        [ComVisible(true)]
        public class GeneralOptions : BaseOptionPage<General> { }
    }

    public class General : BaseOptionModel<General>, IRatingConfig
    {
        [Category("Genral")]
        [DisplayName("Enabled")]
        [Description("Allows you to toggle the rainbow braces.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Category("General")]
        [DisplayName("Timeout (milliseconds)")]
        [Description("Controls the debounce timeout.")]
        [DefaultValue(250)]
        public int Timeout { get; set; } = 250;

        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
