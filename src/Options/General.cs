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
        [Browsable(false)]
        public bool Enabled { get; set; } = true;

        [Category("General")]
        [DisplayName("Timeout (milliseconds)")]
        [Description("Controls the debounce timeout.")]
        [DefaultValue(500)]
        [Browsable(false)]
        public int Timeout { get; set; } = 400;

        [Category("General")]
        [DisplayName("Cycle length")]
        [Description("Coloring will repeat after this many nested braces.")]
        [DefaultValue(4)]
        public int CycleLength { get; set; } = 4;

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
        
        [Category("Braces and brackets")]
        [DisplayName("Colorize angle brackets < >")]
        [Description("Determines whether or not angle brackets should be colorized.")]
        [DefaultValue(true)]
        public bool AngleBrackets { get; set; } = true;

        [Category("Braces and brackets")]
        [DisplayName("(Experimental) Colorize vertical lines between { }")]
        [Description("Determines whether or not colorize vertical linese between curly brackets with the same color as curly brackets pair. Feature is still work in progress.")]
        [DefaultValue(false)]
        public bool VerticalAdornments { get; set; } = false;

        // Used for the rating prompt
        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
