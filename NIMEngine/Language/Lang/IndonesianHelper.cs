using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class IndonesianHelper : I_Language
    {
        public string Code { get; } = "id";
        public string LanguageName { get; } = "Indonesian";
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions
            "dan","atau","tetapi","tapi","namun","karena","sebab","jika","kalau",
            "ketika","saat","sementara","meskipun","walaupun","sehingga","maka",
            "bahwa","agar","supaya","seperti","sebagai","juga","serta","lalu",
            "kemudian","bahkan","hanya","saja","bahwa",

            // Prepositions
            "di","ke","dari","pada","dalam","dengan","untuk","bagi","oleh","tentang",
            "mengenai","terhadap","antara","sejak","sampai","hingga","selama",
            "setelah","sebelum","tanpa","selain","menurut","melalui","tentang",
            "atas","bawah","depan","belakang","samping","luar","dalam",

            // Personal and reflexive pronouns
            "saya","aku","kami","kita","anda","kamu","engkau","dia","ia","mereka",
            "beliau","diriku","dirimu","dirinya","diri","sendiri","-ku","-mu","-nya",

            // Demonstrative pronouns and adjectives
            "ini","itu","sini","situ","sana","begini","begitu","demikian",
            "tersebut","yang",

            // Interrogative and relative words
            "apa","siapa","mana","kapan","mengapa","kenapa","bagaimana","berapa",
            "yang","dimana","kemanakah","darimana","apakah","siapakah",

            // Negative and indefinite words
            "tidak","bukan","belum","jangan","tanpa","tak","tiada",
            "sesuatu","seseorang","beberapa","banyak","sedikit","semua","seluruh",
            "setiap","tiap","masing-masing","lain","lainnya","segala",

            // Auxiliary / modal / copula words
            "adalah","ialah","merupakan","ada","tidak ada","bisa","dapat","mampu",
            "harus","wajib","perlu","mungkin","akan","sedang","telah","sudah",
            "pernah","masih","tetap","harus","boleh","ingin","mau","hendak",

            // Common adverbs
            "sangat","amat","terlalu","cukup","lebih","kurang","paling","hampir",
            "segera","sering","jarang","kadang","selalu","pernah","tidak pernah",
            "sekarang","tadi","nanti","besok","kemarin","hari ini","malam",
            "pagi","siang","sore","malam","sudah","belum","baru","lama",

            // Discourse markers
            "jadi","karena itu","oleh karena itu","namun demikian","meskipun demikian",
            "sebaliknya","selain itu","lebih lanjut","sebagai contoh","misalnya",
            "yaitu","yakni","dengan kata lain","singkatnya","akhirnya"
        };

        // Common Indonesian function words (used by the keyword regex)
        private static readonly Regex IndonesianKeywordRegex = new Regex(
            @"\b(atau|dan|dari|ke|di|ini|itu|untuk|yang|dengan|tidak|adalah|akan|pada|dalam|saya|kami|kita|mereka|dia)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Indonesian / Malay disambiguation ----

        // Indonesian-specific markers (distinct from Malay)
        private static readonly Regex IndonesianMarkerRegex = new Regex(
            @"\b(saya|kami|kita|anda|mereka|dia|ini|itu|tidak|bisa|sudah|belum|sedang|adalah|karena|tetapi|namun|bagaimana|mengapa)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Malay-specific markers (distinct from Indonesian)
        private static readonly Regex MalayMarkerRegex = new Regex(
            @"\b(aku|kami|kita|awak|anda|mereka|ia|ini|itu|tidak|boleh|sudah|belum|sedang|ialah|kerana|tetapi|namun|bagaimana|mengapa|sahaja|je)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return IndonesianKeywordRegex.Matches(Input).Count;
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
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double VariantSignalScore = 0.65;

            if (string.IsNullOrWhiteSpace(Input))
                return MaxFailScore;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return MaxFailScore;

            // Base score: Indonesian has no special diacritics, so use keyword density
            double Score = 0;

            bool HasMarker = IndonesianMarkerRegex.IsMatch(Input);

            // Variant signal: Indonesian markers boost confidence
            if (HasMarker)
                Score = Math.Max(Score, VariantSignalScore);

            int KeywordHits = CountKeywordHits(Input);

            // Keyword density contributes to base score
            double KeywordDensity = (double)KeywordHits / Math.Max(1, LetterCount / 5); // rough words estimate
            Score = Math.Max(Score, Math.Min(1.0, KeywordDensity));

            // One Indonesian keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // No marker and no keyword hit -> treat as fail, capped at MaxFailScore
            if (!HasMarker && KeywordHits == 0)
                return MaxFailScore;

            return Math.Min(1.0, Score);
        }
    }
}