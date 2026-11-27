using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class TraditionalChineseHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        // Asian language detection does not use the stop-word algorithm.
        // Stop words are only used for detection; text processing still uses the standard sliding N-gram algorithm.
        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Pronouns
            "我","你","他","她","它","我們","你們","他們","她們","它們","咱們",
            "自己","別人","大家","誰","什麼","哪","哪兒","哪裡","這","那","這些","那些",

            // Conjunctions
            "和","與","及","或","或者","但","但是","可是","然而","不過","因為","所以",
            "如果","雖然","即使","既然","只要","只有","而且","並且","然後","接著","於是",

            // Particles / structural
            "的","地","得","了","著","過","嗎","呢","吧","啊","呀","嘛","哦","嗯",
            "是","不","沒","沒有","別","被","把","給","讓","叫","對","向","從","到",

            // Prepositions
            "在","於","以","為","為了","關於","對於","按照","根據","通過","經過",
            "沿著","朝著","隨著","除了","包括","除非","作為",

            // Common adverbs
            "很","非常","太","更","最","都","也","還","又","再","才","就","只","僅",
            "已經","曾經","正在","將要","馬上","立刻","剛","剛剛","常常","經常",
            "總是","一直","永遠","從來","偶爾","有時","大概","也許","可能","一定",

            // Numbers / quantifiers
            "一","二","三","四","五","六","七","八","九","十","百","千","萬","億",
            "些","個","位","名","件","條","隻","張","本","次","回","遍","種","類"
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

        private static readonly Regex TraditionalOnlyRegex = new Regex(
            @"[這個們說時國會發對為與從東門馬鳥龍見覺學習問題電話車買賣長開關頭]",
            RegexOptions.Compiled);

        private static readonly Regex SimplifiedOnlyRegex = new Regex(
            @"[这个们说时国会发对为与从东门马鸟龙见觉学习问题电话车买卖长开关头]",
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
            return (int)Languages.TraditionalChinese;
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

            bool HasTraditional = TraditionalOnlyRegex.IsMatch(Input);
            bool HasSimplified = SimplifiedOnlyRegex.IsMatch(Input);

            // Strong traditional signal from static regex
            if (HasTraditional)
            {
                Score = Math.Max(Score, VariantSignalScore);
            }
            else if (HasSimplified)
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            // ---- DB-backed traditional signal (extra weight) ----
            // ChineseVariantMap.CheckLangType already encodes the same rule:
            // RamWords hit OR MatchType=0 DB hit => Traditional.
            ZHType VariantType = ChineseVariantMap.CheckLangType(Input);
            if (VariantType == ZHType.Traditional)
            {
                // DB/RamWords hit is a stronger signal than the static regex:
                // raise to VariantSignalScore then add DbHitBonus.
                Score = Math.Max(Score, VariantSignalScore) + DbHitBonus;
            }
            else if (VariantType == ZHType.Simplified)
            {
                // Explicit simplified verdict from the DB layer caps the score.
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

            // Final caps: simplified-only or kana/hangul keep score below PassScore
            if (!HasTraditional && HasSimplified && VariantType != ZHType.Traditional)
                Score = Math.Min(Score, MaxFailScore);

            if (JapaneseKanaRegex.IsMatch(Input) || KoreanHangulRegex.IsMatch(Input))
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}