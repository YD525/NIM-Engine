using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class KoreanHelper : I_Language
    {
        public string Code { get; } = "ko";
        public string LanguageName { get; } = "Korean";
        public double PassScore { get; set; } = 0.5;
        public float Version { get; set; } = 1.1f;

        // Korean stop words are only used to assist language detection.
        // Text processing still uses the standard sliding N-gram algorithm.
        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.Ordinal)
        {
            // Particles
            "에서","에게","한테","께서","으로","까지","부터","처럼","보다",
            "하고","이나","라고","마다","조차","밖에","만큼","대로",

            // Sentence and connective endings
            "습니다","입니다","합니다","세요","어요","아요","예요","이에요",
            "네요","지만","면서","니까","는데",

            // Common verbs and adjectives
            "하다","하는","한다","했다","있다","있는","없다","없는",
            "되다","된다","됐다","아니다",

            // Conjunctions
            "그리고","하지만","그러나","그래서","그런데","또는","또한","때문","만약",

            // Demonstratives
            "이것","그것","저것","여기","거기","저기","이런","그런","저런",

            // Question words
            "무엇","누구","어디","언제","어떻게","얼마",

            // Pronouns
            "우리","당신","너희","그녀",

            // Common adverbs and time words
            "지금","오늘","내일","어제","정말","아주","매우","너무","다시","아직","이미",

            // Responses
            "아니요"
        };

        // Hangul syllables, Jamo, compatibility Jamo and half-width Jamo
        private static readonly Regex KoreanCharRegex = new Regex(
            @"[\uAC00-\uD7A3\u1100-\u11FF\u3131-\u318E\uA960-\uA97F\uD7B0-\uD7FF\uFFA0-\uFFDC]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Korean;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return StopWords.Count(Word => Input.IndexOf(Word, StringComparison.Ordinal) >= 0);
        }

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
            const double MinHangulRatio = 0.1;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int HangulCount = KoreanCharRegex.Matches(Input).Count;

            if (HangulCount == 0)
                return 0;

            // Spaces, digits and punctuation are not counted
            int LetterCount = Input.Count(char.IsLetter);

            double HangulRatio = (double)HangulCount / Math.Max(LetterCount, HangulCount);

            double Score = HangulRatio >= MinHangulRatio
                ? PassScore + (1 - PassScore) * (HangulRatio - MinHangulRatio) / (1 - MinHangulRatio)
                : MaxFailScore * HangulRatio / MinHangulRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}