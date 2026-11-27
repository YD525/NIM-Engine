using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;

namespace NIMEngine.Language
{
    internal static class EnglishHelper
    {
        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
       {
            "a","an","the",
            "and","or","but","if","then","else","while","when","because","since","unless",

            "on","in","at","to","for","with","by","of","from","into","onto","upon",
            "about","around","through","across","between","among","against",
            "over","under","above","below","before","after","during","within","without",

            "i","me","my","mine","myself",
            "you","your","yours","yourself","yourselves",
            "he","him","his","himself",
            "she","her","hers","herself",
            "it","its","itself",
            "we","us","our","ours","ourselves",
            "they","them","their","theirs","themselves",
            "this","that","these","those","which","who","whom","whose","what",

            "is","are","was","were","be","been","being",
            "has","have","had","having",
            "do","does","did","doing",

            "can","could","may","might","must",
            "will","would","shall","should",

            "not","no","nor","never","none","nothing","nobody",

            "very","too","so","just","only","even","still","also","already","yet",
            "again","ever","always","often","sometimes","usually","rarely",

            "all","any","some","each","every","either","neither",
            "much","many","few","less","more","most","several",

            "now","today","tomorrow","yesterday",

            "as","than","like","via","per"
       };


        private static readonly Regex EnglishRegex = new Regex(
            @"^[a-zA-Z0-9\s\p{P}]+$",
            RegexOptions.Compiled);

        /// <summary>
        /// Determines whether the input string is likely to be English text,
        /// containing only English letters, digits, whitespace, and punctuation.
        /// Excludes accented characters to improve accuracy.
        /// </summary>
        /// <param name="Input">Input text to check</param>
        /// <returns>True if input is likely English; otherwise false.</returns>
        public static bool IsProbablyEnglish(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input))
                return false;

            return EnglishRegex.IsMatch(Input);
        }
    }
}
