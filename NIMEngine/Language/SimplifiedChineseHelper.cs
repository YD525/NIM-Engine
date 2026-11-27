using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class SimplifiedChineseHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        // Asian language detection does not use the stop-word algorithm.
        // Stop words are only used for detection; text processing still uses the standard sliding N-gram algorithm.
        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pronouns
            "我","你","他","她","它","我们","你们","他们","她们","它们","咱们",
            "自己","别人","大家","谁","什么","哪","哪儿","哪里","这","那","这些","那些",

            // Conjunctions
            "和","与","及","或","或者","但","但是","可是","然而","不过","因为","所以",
            "如果","虽然","即使","既然","只要","只有","而且","并且","然后","接着","于是",

            // Particles / structural
            "的","地","得","了","着","过","吗","呢","吧","啊","呀","嘛","哦","嗯",
            "是","不","没","没有","别","被","把","给","让","叫","对","向","从","到",

            // Prepositions
            "在","于","以","为","为了","关于","对于","按照","根据","通过","经过",
            "沿着","朝着","随着","除了","包括","除非","作为",

            // Common adverbs
            "很","非常","太","更","最","都","也","还","又","再","才","就","只","仅",
            "已经","曾经","正在","将要","马上","立刻","刚","刚刚","常常","经常",
            "总是","一直","永远","从来","偶尔","有时","大概","也许","可能","一定",

            // Numbers / quantifiers
            "一","二","三","四","五","六","七","八","九","十","百","千","万","亿",
            "些","个","位","名","件","条","只","张","本","次","回","遍","种","类"
        };

        private static readonly Regex CjkCharRegex = new Regex(
            @"[\u4E00-\u9FFF]",
            RegexOptions.Compiled);

        private static readonly Regex CjkExtARegex = new Regex(
            @"[\u3400-\u4DBF]",
            RegexOptions.Compiled);

        private static readonly Regex CjkCompatRegex = new Regex(
            @"[\uF900-\uFAFF]",
            RegexOptions.Compiled);

        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Simplified / Traditional disambiguation ----

        private static readonly Regex SimplifiedOnlyRegex = new Regex(
            @"[这个们说时国会发对为与从东门马鸟龙见觉学习问题电话车买卖长开关头]",
            RegexOptions.Compiled);

        private static readonly Regex TraditionalOnlyRegex = new Regex(
            @"[這個們說時國會發對為與從東門馬鳥龍見覺學習問題電話車買賣長開關頭]",
            RegexOptions.Compiled);

        // ---- Japanese / Korean negative signals ----

        private static readonly Regex JapaneseKanaRegex = new Regex(
            @"[\u3040-\u30FF]",
            RegexOptions.Compiled);

        private static readonly Regex KoreanHangulRegex = new Regex(
            @"[\uAC00-\uD7AF\u1100-\u11FF]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.SimplifiedChinese;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountCjkChars(string Input)
        {
            return CjkCharRegex.Matches(Input).Count
                 + CjkExtARegex.Matches(Input).Count
                 + CjkCompatRegex.Matches(Input).Count;
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
            const double MinCjkRatio = 0.3;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double VariantSignalScore = 0.65;
            const double DbHitBonus = 0.15;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int CjkCount = CountCjkChars(Input);
            double CjkRatio = (double)CjkCount / LetterCount;

            // Base score driven by CJK character ratio
            double Score = CjkRatio >= MinCjkRatio
                ? PassScore + (1 - PassScore) * (CjkRatio - MinCjkRatio) / (1 - MinCjkRatio)
                : MaxFailScore * CjkRatio / MinCjkRatio;

            bool HasSimplified = SimplifiedOnlyRegex.IsMatch(Input);
            bool HasTraditional = TraditionalOnlyRegex.IsMatch(Input);

            // Strong simplified signal from static regex
            if (HasSimplified)
            {
                Score = Math.Max(Score, VariantSignalScore);
            }
            else if (HasTraditional)
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            // ---- DB-backed variant signal ----
            // CheckLangType returns Traditional when RamWords or MatchType=0 DB hits;
            // returns Simplified when CJK present but no traditional hit.
            ZHType VariantType = ChineseVariantMap.CheckLangType(Input);
            if (VariantType == ZHType.Simplified)
            {
                // Explicit simplified verdict from the DB layer: extra weight,
                // symmetric with TraditionalChineseHelper's DbHitBonus.
                Score = Math.Max(Score, VariantSignalScore) + DbHitBonus;
            }
            else if (VariantType == ZHType.Traditional)
            {
                // DB/RamWords hit traditional characters: cap below PassScore.
                Score = Math.Min(Score, MaxFailScore);
            }

            // Japanese / Korean negative signal
            if (JapaneseKanaRegex.IsMatch(Input) || KoreanHangulRegex.IsMatch(Input))
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            int KeywordHits = StopWords.Count(w => Input.Contains(w));
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Final caps: traditional-only or kana/hangul keep score below PassScore
            if (!HasSimplified && HasTraditional && VariantType != ZHType.Simplified)
                Score = Math.Min(Score, MaxFailScore);

            if (JapaneseKanaRegex.IsMatch(Input) || KoreanHangulRegex.IsMatch(Input))
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}