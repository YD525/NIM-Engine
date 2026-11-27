using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class JapaneseHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        // Asian language detection does not use the stop-word algorithm.
        // Stop words are only used for detection; text processing still uses the standard sliding N-gram algorithm.
        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.Ordinal)
        {
        // Particles and particle phrases
        "から","まで","より","だけ","しか","など","ほど","くらい","ぐらい","ばかり","こそ",
        "でも","ても","けど","けれど","けれども","のに","ので",
        "について","として","による","によって","にとって","において",

        // Copula and auxiliary verbs
        "です","ます","でした","ました","ません","でしょう","ましょう",
        "だった","である","なかった","たい","れる","られる","せる","させる",

        // Common verbs
        "する","した","して","しない","され","させ",
        "いる","いた","いて","いない",
        "ある","あった","ない",
        "なる","なった","できる","できない",

        // Conjunctions
        "そして","しかし","また","または","それで","だから","それから",
        "ところが","つまり","なぜなら","ただし","もし",

        // Demonstratives
        "これ","それ","あれ","どれ",
        "この","その","あの","どの",
        "ここ","そこ","あそこ","どこ",
        "こう","そう","ああ","どう",
        "こんな","そんな","あんな","どんな",

        // Question words
        "なに","なん","なぜ","いつ","だれ","どうして",

        // Formal nouns and set phrases
        "こと","もの","ため","よう","という","といった",

        // Responses
        "はい","いいえ"
        };

        // Hiragana, katakana, extended and half-width kana, plus a few Japanese symbols.
        // The middle dot (U+30FB) and U+30A0 are excluded: Chinese names often use the dot.
        private static readonly Regex JapaneseCharRegex = new Regex(
            @"[\u3041-\u309F\u30A1-\u30FA\u30FC-\u30FF\u31F0-\u31FF\uFF66-\uFF9F々〆]",
            RegexOptions.Compiled);

        public float Version { get; set; } = 1.1f;

        public int GetLanguage()
        {
            return (int)Languages.Japanese;
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
            const double MinKanaRatio = 0.1;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int KanaCount = JapaneseCharRegex.Matches(Input).Count;

            if (KanaCount == 0)
                return 0;

            // Spaces, digits and punctuation are not counted
            int LetterCount = Input.Count(char.IsLetter);

            double KanaRatio = (double)KanaCount / Math.Max(LetterCount, KanaCount);

            double Score = KanaRatio >= MinKanaRatio
                ? PassScore + (1 - PassScore) * (KanaRatio - MinKanaRatio) / (1 - MinKanaRatio)
                : MaxFailScore * KanaRatio / MinKanaRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}