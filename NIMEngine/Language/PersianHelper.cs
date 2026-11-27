using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class PersianHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / particles
            "و","یا","اما","ولی","که","اگر","چون","زیرا","پس","تا","هم","نیز",
            "بلکه","لیکن","هرچند","گرچه","بنابراین","درنتیجه","یعنی","مثلا","مانند",

            // Prepositions
            "در","به","از","با","برای","بر","بدون","بین","زیر","روی","درباره",
            "نزد","پیش","طبق","مثل","غیر","جز","الا","سوی","سو","جانب",

            // Object marker / copula / auxiliary
            "را","است","هست","بود","بودند","شد","شدند","می","نمی","خواهد","خواهند",
            "باید","نباید","شاید","می‌توان","نمی‌توان","دارد","دارند","داشت","داشتند",

            // Personal and reflexive pronouns
            "من","مرا","به‌من","ازمن",
            "تو","تورا","به‌تو","ازتو",
            "او","اورا","به‌او","ازاو",
            "ما","مارا","به‌ما","ازما",
            "شما","شمارا","به‌شما","ازشما",
            "آنها","ایشان","آنان","اینا","اونها",
            "خود","خودم","خودت","خودش","خودمان","خودتان","خودشان",

            // Demonstrative pronouns and adjectives
            "این","آن","اینها","اینجا","آنجا","چنین","چنان","همین","همان",

            // Interrogative and relative words
            "چه","چی","کی","کجا","کدام","چطور","چگونه","چرا","کِی","چند","چقدر",
            "آیا","مگر","کیست","چیست","کدامین",

            // Negative and indefinite words
            "نه","هیچ","هیچ‌کس","هیچ‌چیز","هیچوقت","هرگز","بی","بدون",
            "کسی","چیزی","بعضی","برخی","همه","هر","هریک","دیگر","دیگری",

            // Common adverbs
            "خیلی","بسیار","کم","زیاد","بیشتر","کمتر","الان","اکنون","حالا",
            "دیروز","امروز","فردا","قبلا","بعد","قبل","همیشه","گاهی","اغلب",
            "دوباره","باز","هنوز","فقط","تنها","حتی","همچنین","البته",
            "حتما","شاید","اصلا","واقعا","تقریبا","کاملا","جدا",

            // Discourse markers
            "بنابراین","درنتیجه","به‌هرحال","به‌طور","خلاصه"
        };

        private static readonly Regex PersianKeywordRegex = new Regex(
            @"\b(و|در|به|از|این|آن|برای|که|با|یک|را|است|می|شد|بر|هم|تا)\b",
            RegexOptions.Compiled);

        private static readonly Regex PersianCharRegex = new Regex(
            "[\u0600-\u06FF]",
            RegexOptions.Compiled);

        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Persian / Arabic disambiguation ----

        // Strong Persian signals
        private static readonly Regex PersianStrongLetterRegex = new Regex(
            @"[پچژگ]",
            RegexOptions.Compiled);

        private static readonly Regex PersianStrongDigitRegex = new Regex(
            @"[\u06F0-\u06F9]",
            RegexOptions.Compiled);

        // Weak Persian signals (occur in Persian but less exclusively)
        private static readonly Regex PersianWeakLetterRegex = new Regex(
            @"[کی]",
            RegexOptions.Compiled);

        // Weak Arabic signals (old Persian texts / input methods may use them)
        private static readonly Regex ArabicWeakLetterRegex = new Regex(
            @"[يك]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Persian;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return PersianKeywordRegex.Matches(Input).Count;
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
            const double MinPersianRatio = 0.3;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double StrongSignalScore = 0.7;
            const double WeakSignalFactor = 0.85;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int PersianCharCount = PersianCharRegex.Matches(Input).Count;
            double PersianCharRatio = (double)PersianCharCount / LetterCount;

            double Score = PersianCharRatio >= MinPersianRatio
                ? PassScore + (1 - PassScore) * (PersianCharRatio - MinPersianRatio) / (1 - MinPersianRatio)
                : MaxFailScore * PersianCharRatio / MinPersianRatio;

            bool HasStrongPersian = PersianStrongLetterRegex.IsMatch(Input) || PersianStrongDigitRegex.IsMatch(Input);
            bool HasWeakArabic = ArabicWeakLetterRegex.IsMatch(Input);

            // Strong Persian signal: raise to 0.7 and immune to weak Arabic penalty
            if (HasStrongPersian)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Weak Arabic signal only when no strong Persian evidence
            else if (HasWeakArabic)
            {
                Score *= WeakSignalFactor;
            }

            int KeywordHits = CountKeywordHits(Input);

            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}