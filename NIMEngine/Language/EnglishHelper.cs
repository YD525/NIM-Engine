using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class EnglishHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles
            "a","an","the",

            // Conjunctions
            "and","or","but","if","then","else","while","when","because","since","unless",

            // Prepositions
            "on","in","at","to","for","with","by","of","from","into","onto","upon",
            "about","around","through","across","between","among","against",
            "over","under","above","below","before","after","during","within","without",

            // Pronouns
            "i","me","my","mine","myself",
            "you","your","yours","yourself","yourselves",
            "he","him","his","himself",
            "she","her","hers","herself",
            "it","its","itself",
            "we","us","our","ours","ourselves",
            "they","them","their","theirs","themselves",
            "this","that","these","those","which","who","whom","whose","what",

            // Question words and places
            "where","why","how","here","there",

            // Auxiliary and modal verbs
            "am","is","are","was","were","be","been","being",
            "has","have","had","having",
            "do","does","did","doing",
            "can","could","may","might","must",
            "will","would","shall","should",

            // Negations
            "not","no","nor","never","none","nothing","nobody",

            // Common adverbs
            "very","too","so","just","only","even","still","also","already","yet",
            "again","ever","always","often","sometimes","usually","rarely",

            // Quantifiers
            "all","any","some","each","every","either","neither",
            "much","many","few","less","more","most","several",

            // Time words
            "now","today","tomorrow","yesterday",

            // Other common words
            "as","than","like","via","per"
        };

        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{M}]+", RegexOptions.Compiled);

        // Basic Latin letters, accented letters found in English loanwords,
        // digits, whitespace and punctuation
        private static readonly Regex EnglishRegex = new Regex(
            @"^[a-zA-Z0-9éÉèÈêÊëËïÏàÀâÂçÇ\s\p{P}]+$",
            RegexOptions.Compiled);

        public float Version { get; set; } = 1.1f;

        public int GetLanguage()
        {
            return (int)Languages.English;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        // Fallback language: priority is handled by the caller.
        // Matching the character set is enough, with or without stop words.
        public bool IsProbablyMatch(string Input)
        {
            return GetProbability(Input) >= PassScore;
        }

        /// <summary>
        /// Returns a score from 0 to 1. A score of PassScore or higher is a match.
        /// </summary>
        /// <param name="Input">Input text</param>
        public double GetProbability(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input) || !EnglishRegex.IsMatch(Input))
                return 0;

            int WordCount = 0;
            int Hits = 0;

            foreach (Match Word in WordRegex.Matches(Input))
            {
                WordCount++;

                if (StopWords.Contains(Word.Value))
                    Hits++;
            }

            if (WordCount == 0)
                return PassScore;

            // Matching text starts at PassScore, stop words raise the score
            return PassScore + (1 - PassScore) * Hits / WordCount;
        }
    }
}