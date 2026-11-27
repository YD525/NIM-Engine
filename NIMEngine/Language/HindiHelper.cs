using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class HindiHelper : I_Language
    {
        public string Code { get; } = "hi";
        public string LanguageName { get; } = "Hindi";

        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / connectors
            "और","या","लेकिन","मगर","परंतु","किंतु","अगर","तो","फिर","क्योंकि",
            "जब","जबकि","हालांकि","ताकि","इसलिए","इसलिए","यानी","जैसे","जैसा",

            // Postpositions (Hindi uses postpositions)
            "में","के","का","की","से","पर","तक","को","ने","कि","लिए","साथ",
            "बिना","बाद","पहले","दौरान","बारे","मुताबिक","खिलाफ","पास",
            "अंदर","बाहर","ऊपर","नीचे","आगे","पीछे","बीच","बीच में",

            // Personal and reflexive pronouns
            "मैं","हम","तू","तुम","आप","वह","यह","उन","उन्हें","उनका",
            "उनकी","उनके","हमें","हमारा","हमारी","हमारे","तुम्हें","तुम्हारा",
            "तुम्हारी","तुम्हारे","आपका","आपकी","आपके","अपना","अपनी","अपने",
            "खुद","स्वयं",

            // Demonstrative pronouns and adjectives
            "यह","वह","यहाँ","वहाँ","यही","वही","ऐसा","वैसा","ऐसे","वैसे",
            "इतना","इतनी","इतने","जितना","जितनी","जितने",

            // Interrogative and relative words
            "क्या","कौन","कहाँ","कब","क्यों","कैसे","कैसा","कैसी","कितने","कितनी",
            "कौनसा","कौनसी","कोई","कुछ","किस","जिस","जो","जहाँ","जब","जैसे",

            // Negative and indefinite words
            "नहीं","न","कभी","कोई","कुछ","सब","सारे","हर","हरकोई",
            "कुछ","दूसरा","दूसरी","दूसरे","और","ज़्यादा","कम",

            // Auxiliary / modal / copula words
            "है","हैं","था","थी","थे","हो","होगा","होगी","हूँ","होंगे",
            "कर","करता","करती","करते","किया","की","किए","जाता","जाती","जाते",
            "सकता","सकती","सकते","चाहिए","पड़ता","पड़ती","पड़ते",

            // Common adverbs
            "बहुत","कम","ज़्यादा","अधिक","कम","अब","अभी","फिर","हमेशा","कभी",
            "अक्सर","शायद","ज़रूर","बिल्कुल","लगभग","सिर्फ","भी","ही",
            "आज","कल","परसों","सुबह","शाम","रात","दोपहर",

            // Discourse markers
            "इसलिए","इसीलिए","बहरहाल","मिसाल","यानी","संक्षेप","अंत में",
            "इसके अलावा","विशेष रूप से","सामान्य रूप से"
        };

        // Common Hindi function words / postpositions (used by the keyword regex)
        private static readonly Regex HindiKeywordRegex = new Regex(
            @"(है|में|के|और|का|की|से|यह|मैं|तुम|यहाँ|नहीं|क्या|वह|हैं|था|थी|थे|को|ने|पर|तक)",
            RegexOptions.Compiled);

        // Devanagari Unicode block: U+0900 – U+097F
        private static readonly Regex HindiCharRegex = new Regex(
            @"[\u0900-\u097F]",
            RegexOptions.Compiled);

        // Letters (Devanagari + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}\p{M}]",
            RegexOptions.Compiled);

        // ---- Hindi / Urdu disambiguation ----

        // Devanagari-specific characters (Hindi) — the script itself is the strongest signal
        private static readonly Regex DevanagariSpecificRegex = new Regex(
            @"[\u0900-\u097F]",
            RegexOptions.Compiled);

        // Arabic-script characters (Urdu) — their presence suggests NOT Hindi
        private static readonly Regex ArabicScriptRegex = new Regex(
            @"[\u0600-\u06FF]",
            RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return HindiKeywordRegex.Matches(Input).Count;
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
            const double MinHindiRatio = 0.3;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double ScriptSignalScore = 0.8;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int HindiCharCount = HindiCharRegex.Matches(Input).Count;
            double HindiCharRatio = (double)HindiCharCount / LetterCount;

            // Base score driven by Devanagari character ratio
            double Score = HindiCharRatio >= MinHindiRatio
                ? PassScore + (1 - PassScore) * (HindiCharRatio - MinHindiRatio) / (1 - MinHindiRatio)
                : MaxFailScore * HindiCharRatio / MinHindiRatio;

            // Strong script signal: Devanagari characters present at all
            if (DevanagariSpecificRegex.IsMatch(Input))
            {
                Score = Math.Max(Score, ScriptSignalScore);
            }
            // Arabic-script characters without Devanagari: likely Urdu, not Hindi
            else if (ArabicScriptRegex.IsMatch(Input))
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            int KeywordHits = CountKeywordHits(Input);

            // One Hindi keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Arabic-script-only text caps final score below PassScore
            if (!DevanagariSpecificRegex.IsMatch(Input) && ArabicScriptRegex.IsMatch(Input))
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}