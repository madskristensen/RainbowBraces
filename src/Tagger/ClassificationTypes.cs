using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RainbowBraces
{
    public class ClassificationTypes
    {
        public static string GetName(int level)
        {
            int mod = level % 4;

            if (mod == 0)
            {
                mod = 4;
            }

            return mod switch
            {
                1 => Level1,
                2 => Level2,
                3 => Level3,
                _ =>    Level4
            };
        }

        public const string Level1 = "Rainbow Brace level 1";
        public const string Level2 = "Rainbow Brace level 2";
        public const string Level3 = "Rainbow Brace level 3";
        public const string Level4 = "Rainbow Brace level 4";

        [Export, Name(Level1)]
        internal static ClassificationTypeDefinition Level1Classification = null;

        [Export, Name(Level2)]
        internal static ClassificationTypeDefinition Level2Classification = null;

        [Export, Name(Level3)]
        internal static ClassificationTypeDefinition Level3Classification = null;

        [Export, Name(Level4)]
        internal static ClassificationTypeDefinition Level4Classification = null;
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypes.Level1)]
    [Name(ClassificationTypes.Level1)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class Level1 : ClassificationFormatDefinition
    {
        public Level1()
        {
            ForegroundColor = Colors.Orange;
            DisplayName = ClassificationTypes.Level1;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypes.Level2)]
    [Name(ClassificationTypes.Level2)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class Level2 : ClassificationFormatDefinition
    {
        public Level2()
        {
            ForegroundColor = Colors.Green;
            DisplayName = ClassificationTypes.Level2;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypes.Level3)]
    [Name(ClassificationTypes.Level3)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class Level3 : ClassificationFormatDefinition
    {
        public Level3()
        {
            ForegroundColor = Colors.PaleVioletRed;
            DisplayName = ClassificationTypes.Level3;
        }
    }

    [Export(typeof(EditorFormatDefinition))]
    [ClassificationType(ClassificationTypeNames = ClassificationTypes.Level4)]
    [Name(ClassificationTypes.Level4)]
    [UserVisible(true)]
    [Order(After = Priority.High)]
    internal sealed class Level4 : ClassificationFormatDefinition
    {
        public Level4()
        {
            ForegroundColor = Colors.CornflowerBlue;
            DisplayName = ClassificationTypes.Level4;
        }
    }}
