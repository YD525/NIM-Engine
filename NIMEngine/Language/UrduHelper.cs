using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class UrduHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / connectors
            "اور","یا","لیکن","مگر","بلکہ","اگر","تو","پھر","کیونکہ","جب","جبکہ",
            "حالانکہ","تاہم","اسی","لیے","تاکہ","چنانچہ","بنابریں","یعنی","مثلا",

            // Postpositions (Urdu uses postpositions, not prepositions)
            "میں","کے","کا","کی","سے","پر","تک","کو","نے","کہ","لیے","ساتھ",
            "بغیر","بعد","پہلے","دوران","بارے","مطابق","خلاف","قریب","پاس",
            "اندر","باہر","اوپر","نیچے","آگے","پیچھے","درمیان","درمیان",

            // Personal and reflexive pronouns
            "میں","ہم","تو","تم","آپ","وہ","یہ","ان","انہیں","انہوں","انکا",
            "انکی","انکے","ہمیں","ہماری","ہمارا","ہمارے","تمہیں","تمہارا",
            "تمہاری","تمہارے","آپکا","آپکی","آپکے","اپنا","اپنی","اپنے",
            "خود","خودم","خودت","خودش",

            // Demonstrative pronouns and adjectives
            "یہ","وہ","یہاں","وہاں","یہی","وہی","ایسا","ویسا","ایسے","ویسے",
            "اتنا","اتنی","اتنے","جتنا","جتنی","جتنے",

            // Interrogative and relative words
            "کیا","کون","کہاں","کب","کیوں","کیسے","کیسا","کیسی","کتنے","کتنی",
            "کونسا","کونسی","کوئی","کچھ","کس","جس","جو","جہاں","جب","جیسے",

            // Negative and indefinite words
            "نہیں","نہ","کبھی","نہیں","کوئی","کچھ","سب","تمام","ہر","ہرکوئی",
            "بعض","کچھ","دوسرا","دوسری","دوسرے","اور","زیادہ","کم",

            // Auxiliary / modal / copula words
            "ہے","ہیں","تھا","تھی","تھے","ہو","ہوگا","ہوگی","ہوں","ہوگے",
            "کر","کرتا","کرتی","کرتے","کیا","کی","کیے","جاتا","جاتی","جاتے",
            "سکتا","سکتی","سکتے","چاہیے","چاہیے","پڑتا","پڑتی","پڑتے",

            // Common adverbs
            "بہت","کم","زیادہ","بیشتر","کمتر","اب","ابھی","پھر","ہمیشہ","کبھی",
            "اکثر","شاید","یقینا","ضرور","بالکل","تقریبا","صرف","بھی","ہی",
            "آج","کل","پرسوں","صبح","شام","رات","دوپہر",

            // Discourse markers
            "لہذا","اسلیے","بہرحال","بہرطور","مثلا","یعنی","خلاصہ","آخرکار",
            "مزید","علاوہ","بالخصوص","خاصطور","عامطور"
        };

        // Common Urdu function words / postpositions (used by the keyword regex)
        private static readonly Regex UrduKeywordRegex = new Regex(
            @"(ہے|میں|کے|اور|کا|کی|سے|یہ|تم|یہاں|نہیں|کیا|وہ|ہیں|تھا|تھی|تھے|کو|نے|پر|تک)",
            RegexOptions.Compiled);

        // Arabic-script block: U+0600 – U+06FF
        private static readonly Regex UrduCharRegex = new Regex(
            @"[\u0600-\u06FF]",
            RegexOptions.Compiled);

        // Letters (Arabic script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Urdu / Arabic / Persian disambiguation ----

        // Urdu-specific letters: ٹ ڈ ڑ ں ے ھ ۓ (retroflex + aspiration + final heh)
        private static readonly Regex UrduSpecificLetterRegex = new Regex(
            @"[ٹڈڑںےھۓ]",
            RegexOptions.Compiled);

        // Urdu-specific digits ۰۱۲۳۴۵۶۷۸۹ share with Persian; Urdu commonly uses Arabic-Indic ٠١٢٣٤٥٦٧٨٩ or Western digits
        private static readonly Regex UrduDigitRegex = new Regex(
            @"[\u0660-\u0669]",
            RegexOptions.Compiled);

        // Persian-only letters پ چ ژ گ — their presence suggests NOT Urdu (but Urdu does borrow some)
        private static readonly Regex PersianOnlyLetterRegex = new Regex(
            @"[پچژگ]",
            RegexOptions.Compiled);

        // Arabic-specific letters ة ى ي ك — rare in Urdu
        private static readonly Regex ArabicSpecificLetterRegex = new Regex(
            @"[ةىيك]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Urdu;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return UrduKeywordRegex.Matches(Input).Count;
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
            const double MinUrduRatio = 0.10;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double StrongSignalScore = 0.7;
            const double WeakSignalFactor = 0.85;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int UrduCharCount = UrduCharRegex.Matches(Input).Count;
            double UrduCharRatio = (double)UrduCharCount / LetterCount;

            // Base score driven by Arabic-script character ratio
            double Score = UrduCharRatio >= MinUrduRatio
                ? PassScore + (1 - PassScore) * (UrduCharRatio - MinUrduRatio) / (1 - MinUrduRatio)
                : MaxFailScore * UrduCharRatio / MinUrduRatio;

            bool HasStrongUrdu = UrduSpecificLetterRegex.IsMatch(Input);
            bool HasStrongArabic = ArabicSpecificLetterRegex.IsMatch(Input);
            bool HasPersianOnly = PersianOnlyLetterRegex.IsMatch(Input);

            // Strong Urdu signal: Urdu-specific letters raise to 0.7
            if (HasStrongUrdu)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Persian-only letters without Urdu-specific letters: weak negative
            else if (HasPersianOnly && !HasStrongArabic)
            {
                Score *= WeakSignalFactor;
            }
            // Arabic-specific letters without Urdu-specific letters: weak negative
            else if (HasStrongArabic && !HasPersianOnly)
            {
                Score *= WeakSignalFactor;
            }

            int KeywordHits = CountKeywordHits(Input);

            // One Urdu keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Strong Urdu signal keeps the score high; no final cap
            return Math.Min(1.0, Score);
        }
    }
}