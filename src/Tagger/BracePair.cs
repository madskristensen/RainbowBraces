using Microsoft.VisualStudio.Text;

namespace RainbowBraces
{
    public class BracePair
    {
        public Span Open { get; set; }
        public Span Close { get; set; }
        public int Level { get; set; }
    }

    public class DummyBracePair : BracePair { }
}
