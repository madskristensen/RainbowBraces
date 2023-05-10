using System.Collections.Generic;
using System.Windows.Markup;
using System.Windows.Media;

namespace RainbowBraces.Helper
{
    internal class FontFamilyMapper
    {
        private static Dictionary<string, Typeface> _cache;

        /// <summary>
        /// Cascadia Code draw sequences of characters differently and can introduce glitches.
        /// For example ">>" is rendered as single glyph with color of the last '>'.
        /// </summary>
        /// <param name="original">Original typeface. If is not of Cascadia Code family, nothing happens.</param>
        /// <param name="equivalent">Equivalent typeface to the original one.</param>
        /// <returns>Returns <see langword="true"/> if equivalent typeface was found.</returns>
        /// <remarks>The method is not thread-safe, it is expected to be called only from the UI thread.</remarks>
        public static bool TryGetEquivalentToCascadiaCode(Typeface original, out Typeface equivalent)
        {
            FontFamily fontFamily = original.FontFamily;
            foreach (KeyValuePair<XmlLanguage, string> kv in fontFamily.FamilyNames)
            {
                string familyName = kv.Value;
                equivalent = familyName switch
                {
                    "Cascadia Code" => GetTypeface("Cascadia Mono"),
                    "Cascadia Code ExtraLight" => GetTypeface("Cascadia Mono ExtraLight"),
                    "Cascadia Code Light" => GetTypeface("Cascadia Mono Light"),
                    "Cascadia Code SemiBold" => GetTypeface("Cascadia Mono SemiBold"),
                    "Cascadia Code SemiLight" => GetTypeface("Cascadia Mono SemiLight"),
                    _ => null
                };
                if (equivalent != null) return true;
            }

            equivalent = null;
            return false;
        }

        private static Typeface GetTypeface(string name)
        {
            _cache ??= new Dictionary<string, Typeface>();
            if (_cache.TryGetValue(name, out Typeface typeface)) return typeface;
            try
            {
                typeface = new Typeface(name);
            }
            catch
            {
                // If for whatever reason Cascadia Code is installed and used but the Cascadia Mono counterpart is not, return null and cache it.
                typeface = null;
            }

            _cache.Add(name, typeface);
            return typeface;
        }
    }
}
