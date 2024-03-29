﻿namespace RainbowBraces
{
	internal static class CustomTextViewRoles
    {
        /// <summary>
        /// TextView role for sticky scroll container.
        /// </summary>
        public const string StickyScroll = "STICKYSCROLL_TEXT_VIEW";

        /// <summary>
        /// TextView role for diff editor.
        /// All diff windows share this role.
        /// </summary>
        public const string Diff = "DIFF";

        /// <summary>
        /// TextView role for inline diff editor.
        /// </summary>
        public const string InlineDiff = "INLINEDIFF";

        /// <summary>
        /// TextView role for Read–eval–print loop.
        /// </summary>
        public const string Repl = "REPL";

        /// <summary>
        /// TextView role for preview of all-in-one search result.
        /// </summary>
        public const string SearchResultPreview = "SEARCH_RESULT_PREVIEW";
    }
}