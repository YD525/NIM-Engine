using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class ArabicHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / connectors
            "و","أو","أما","لكن","بل","ثم","إذا","إن","أن","لأن","حتى","كما","بينما",
            "كلما","كي","لكي","حيث","أي","أيضا","كذلك","إذ","إذن","لذا",

            // Prepositions
            "في","على","إلى","من","عن","مع","بدون","منذ","خلال","بين","تحت",
            "فوق","أمام","خلف","حول","ضد","نحو","عند","لدى","قبل","بعد","حسب",
            "وفق","طوال","دون","سوى","غير","مثل",

            // Particles / question words / negation
            "هل","ما","ماذا","لماذا","كيف","متى","أين","كم","لا","لم","لن",
            "ليس","ليست","سوف","قد","لقد","كأن","ليت","لعل","يا","إلا",

            // Personal and reflexive pronouns
            "أنا","نحن","أنت","أنتم","أنتن","هو","هي","هما","هم","هن",
            "إياه","إياها","إياك","إياي","نفسي","نفسك","نفسه","نفسها","أنفسنا",
            "الذي","التي","الذين","اللاتي","اللائي","اللذان","اللتان","اللواتي",

            // Demonstrative pronouns and adjectives
            "هذا","هذه","هؤلاء","ذلك","تلك","أولئك","هنا","هناك","هنالك",
            "كذا","هكذا","هذان","هاتان",

            // Negative and indefinite words
            "أحد","إحدى","شيء","شيئا","بعض","كل","جميع","كلا","كلتا",
            "مهما","حيثما","أينما",

            // Auxiliary / modal / copula words
            "كان","كانت","كانوا","يكون","تكون","يكونوا","صار","صارت",
            "أصبح","أصبحت","ظل","ظلت","بات","باتت","يمكن","يجب",

            // Common adverbs
            "جدا","كثيرا","قليلا","أكثر","أقل","الآن","حاليا","دائما",
            "أبدا","غالبا","أحيانا","أمس","اليوم","غدا","فقط",
            "أيضا","كذلك","ثم","لاحقا","مؤخرا","أخيرا","أولا",
            "فعلا","حقا","بالطبع","ربما","بالضبط","تقريبا","تماما",

            // Discourse markers
            "إذن","لذا","بالتالي","بالإضافة","علاوة","بالنسبة","بخصوص",
            "فيما","حول","مثلا","أعني","يعني","باختصار","ختاما"
        };

        private static readonly Regex ArabicKeywordRegex = new Regex(
            @"\b(من|في|على|إلى|عن|مع|هذا|هذه|ذلك|التي|الذي|كان|كانت|لا|لم|لن|ما|هو|هي|أنا|نحن|أنت|أنتم|هل|قد|و|أو|لكن|ثم|إذا|إن|أن)\b",
            RegexOptions.Compiled);

        private static readonly Regex ArabicCharRegex = new Regex(
            @"[\u0600-\u06FF]",
            RegexOptions.Compiled);

        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Arabic / Persian disambiguation ----

        // Strong Arabic signals
        private static readonly Regex ArabicStrongLetterRegex = new Regex(
            @"[ةى]",
            RegexOptions.Compiled);

        private static readonly Regex ArabicStrongDigitRegex = new Regex(
            @"[\u0660-\u0669]",
            RegexOptions.Compiled);

        // Weak Arabic signals (old Persian texts / input methods may use them)
        private static readonly Regex ArabicWeakLetterRegex = new Regex(
            @"[يك]",
            RegexOptions.Compiled);

        // Strong Persian signals: their presence strongly suggests NOT Arabic
        private static readonly Regex PersianStrongLetterRegex = new Regex(
            @"[پچژگ]",
            RegexOptions.Compiled);

        private static readonly Regex PersianStrongDigitRegex = new Regex(
            @"[\u06F0-\u06F9]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Arabic;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return ArabicKeywordRegex.Matches(Input).Count;
        }

        public bool IsProbablyMatch(string Input)
        {
            return GetProbability(Input) >= PassScore;
        }

        /// <summary>
        /// Returns a score from 0 to 1. A score of PassScore or higher is a match.
        /// </summary>
        public double GetProbability(string Input)
        {
            const double MinArabicRatio = 0.10;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double StrongSignalScore = 0.7;
            const double WeakSignalFactor = 0.85;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int ArabicCharCount = ArabicCharRegex.Matches(Input).Count;
            double ArabicCharRatio = (double)ArabicCharCount / LetterCount;

            double Score = ArabicCharRatio >= MinArabicRatio
                ? PassScore + (1 - PassScore) * (ArabicCharRatio - MinArabicRatio) / (1 - MinArabicRatio)
                : MaxFailScore * ArabicCharRatio / MinArabicRatio;

            bool HasStrongArabic = ArabicStrongLetterRegex.IsMatch(Input) || ArabicStrongDigitRegex.IsMatch(Input);
            bool HasStrongPersian = PersianStrongLetterRegex.IsMatch(Input) || PersianStrongDigitRegex.IsMatch(Input);
            bool HasWeakArabic = ArabicWeakLetterRegex.IsMatch(Input);

            // Strong Persian signal: cap below PassScore (pure Arabic never contains پ چ ژ گ / ۰–۹)
            if (HasStrongPersian)
            {
                Score = Math.Min(Score, MaxFailScore);
            }
            // Strong Arabic signal: raise to 0.7
            else if (HasStrongArabic)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Weak Arabic signal: small boost
            else if (HasWeakArabic)
            {
                Score = Math.Max(Score, Score * (1.0 + (1.0 - WeakSignalFactor)));
            }

            int KeywordHits = CountKeywordHits(Input);

            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Strong Persian signal caps final score below PassScore even after keyword bonus
            if (HasStrongPersian)
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}