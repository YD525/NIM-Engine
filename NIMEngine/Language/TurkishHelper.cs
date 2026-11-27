using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class TurkishHelper : I_Language
    {
        public string Code { get; } = "tr";
        public string LanguageName { get; } = "Turkish";
        public double PassScore { get; set; } = 0.5;
        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions
            "ve","veya","ya","yahut","ama","fakat","lakin","ancak","çünkü","eğer",
            "ki","hem","de","da","ile","ise","madem","mademki","oysa","oysaki",
            "hatta","yani","öyleyse","halbuki","meğer","nitekim","zira",

            // Postpositions / prepositions
            "için","gibi","kadar","sonra","önce","üzere","göre","rağmen","dolayı",
            "beri","başka","karşı","doğru","dek","değin","diye",
            "arasında","içinde","üzerinde","yanında","altında","önünde","arkasında","hakkında",

            // Particles / question particle / emphasis
            "mi","mı","mu","mü","mıdır","midir","mudur","müdür",
            "değil","artık","hâlâ","hala","henüz","sadece","yalnız","bile","dahi",
            "işte","evet","hayır","belki","elbette","tabii","muhakkak","mutlaka",

            // Personal and reflexive pronouns
            "ben","beni","bana","bende","benden","benim",
            "sen","seni","sana","sende","senden","senin",
            "o","onu","ona","onda","ondan","onun",
            "biz","bizi","bize","bizde","bizden","bizim",
            "siz","sizi","size","sizde","sizden","sizin",
            "onlar","onları","onlara","onlarda","onlardan","onların",
            "kendi","kendim","kendin","kendisi","kendimiz","kendiniz","kendileri",

            // Demonstrative pronouns and adjectives
            "bu","şu","bunlar","şunlar",
            "bunu","şunu","buna","şuna","bunda","şunda","bundan","şundan","bunun","şunun",
            "böyle","şöyle","öyle","böylesi","şöylesi","öylesi",

            // Interrogative and relative words
            "kim","kimi","kime","kimde","kimden","kimin",
            "ne","neyi","neye","nede","neden","neyin","neler",
            "hangi","hangisi","hangileri",
            "nasıl","niçin","niye","nerede","nereye","nereden",
            "kaç","kaçıncı",

            // Negative and indefinite words
            "hiç","hiçbir","hiçkimse","hiçbiri","asla","katiyen",
            "kimse","biri","birisi","bazı","bazısı",
            "herkes","herhangi",

            // Auxiliary / modal / copula words
            "var","yok","olmak","olur","oldu","olmuş","olacak",
            "olabilir","olmalı","olmalıdır","gerek","gerekir","lazım",
            "mümkün","imkânsız","imkansız","zorunda","mecbur",

            // Common adverbs
            "çok","az","biraz","daha","en","pek","fazla",
            "şimdi","sonra","önce","bugün","yarın","dün","gece","gündüz",
            "burada","orada","şurada",
            "hep","bazen","sık","nadiren",
            "yine","tekrar","zaten",
            "gerçekten","doğrusu","aslında",

            // Discourse markers
            "mesela","örneğin","kısaca","sonuçta",
            "ayrıca","üstelik","dolayısıyla","bu","yüzden","nedenle","sebeple"
        };

        // Matches common Turkish function words: conjunctions, pronouns, question particles, etc.
        private static readonly Regex TurkishKeywordRegex = new Regex(
            @"\b(ve|bir|bu|çok|ama|değil|için|ile|sen|ben|mı|mu|mi|mü|şu)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Matches Turkish-specific accented characters: ç, ğ, ı, İ, ö, ş, ü and their uppercase variants
        private static readonly Regex TurkishAccentCharRegex = new Regex("[çğıİöşüÇĞİÖŞÜ]", RegexOptions.Compiled);

        // Latin letters (basic + extended), used as denominator to exclude digits/punctuation
        private static readonly Regex LatinLetterRegex = new Regex(@"[\p{L}]", RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return TurkishKeywordRegex.Matches(Input).Count;
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
            const double MinAccentRatio = 0.01;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = LatinLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int AccentCharCount = TurkishAccentCharRegex.Matches(Input).Count;
            double AccentCharRatio = (double)AccentCharCount / LetterCount;

            // Base score driven by Turkish-specific accented character ratio
            double Score = AccentCharRatio >= MinAccentRatio
                ? PassScore + (1 - PassScore) * (AccentCharRatio - MinAccentRatio) / (1 - MinAccentRatio)
                : MaxFailScore * AccentCharRatio / MinAccentRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One Turkish keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}