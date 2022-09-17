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
        [Category("General")]
        [DisplayName("Enable rainbow braces")]
        [Description("Allows you to toggle the rainbow braces on and off.")]
        [DefaultValue(true)]
        public bool Enabled { get; set; } = true;

        [Category("General")]
        [DisplayName("Max line length")]
        [Description("Maximum line count for files to enable colorization. For performance reasons, we don't want to colorize huge files")]
        [DefaultValue(3000)]
        public int MaxBufferLines { get; set; } = 3000;

        [Category("General")]
        [DisplayName("Timeout (milliseconds)")]
        [Description("Controls the debounce timeout.")]
        [DefaultValue(500)]
        [Browsable(false)]
        public int Timeout { get; set; } = 400;

        [Category("Braces and brackets")]
        [DisplayName("Colorize curly brackets { }")]
        [Description("Determines whether or not curly brackets should be colorized.")]
        [DefaultValue(true)]
        public bool CurlyBrackets { get; set; } = true;

        [Category("Braces and brackets")]
        [DisplayName("Colorize parentheses ( )")]
        [Description("Determines whether or not parentheses should be colorized.")]
        [DefaultValue(true)]
        public bool Parentheses { get; set; } = true;

        [Category("Braces and brackets")]
        [DisplayName("Colorize square brackets [ ]")]
        [Description("Determines whether or not square brackets should be colorized.")]
        [DefaultValue(true)]
        public bool SquareBrackets { get; set; } = true;


        // Used for the rating prompt
        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
