using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class VietnameseHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.0f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / connectors
            "và","hoặc","nhưng","mà","nếu","thì","vì","bởi","nên","do","để","cho",
            "khi","lúc","trong","ngoài","trên","dưới","với","cùng","hay","còn",
            "nên","vậy","thế","như","tuy","dù","mặc","dầu","rằng","là",

            // Prepositions
            "của","ở","tại","vào","ra","lên","xuống","qua","sang","đến","từ",
            "bằng","về","đối","với","giữa","trước","sau","dưới","trên","trong",
            "ngoài","cạnh","gần","xa","theo","về","cho","để","bởi",

            // Personal and reflexive pronouns
            "tôi","mình","ta","chúng","tôi","chúng","ta","bạn","anh","chị","em",
            "ông","bà","cô","chú","bác","họ","nó","ai","mình","tự","bản","thân",

            // Demonstrative pronouns and adjectives
            "này","nọ","kia","đó","đây","ấy","nào","thế","vậy","như","vậy",

            // Interrogative and relative words
            "ai","gì","nào","đâu","sao","thế","bao","nhiêu","mấy","bao","lâu",
            "bao","giờ","tại","sao","vì","sao","thế","nào","ra","sao",

            // Negative and indefinite words
            "không","chẳng","chưa","đừng","chớ","nào","gì","ai","cũng","đều",
            "mọi","các","những","mỗi","từng","vài","mấy","một","ít","nhiều",

            // Auxiliary / modal / copula words
            "là","có","được","bị","phải","nên","cần","muốn","sẽ","đang","đã",
            "vừa","mới","từng","hay","biết","có","thể","không","thể","nên",

            // Common adverbs
            "rất","lắm","quá","hơi","khá","cực","vô","cùng","hơn","kém","nhất",
            "nữa","thêm","bớt","luôn","thường","hay","ít","khi","nào","giờ",
            "đây","đó","nay","qua","mai","hôm","nay","hôm","qua","hôm","kia",
            "sáng","trưa","chiều","tối","đêm",

            // Discourse markers
            "vậy","thế","nên","do","đó","vì","vậy","tuy","nhiên","ngoài","ra",
            "hơn","nữa","thêm","vào","đó","tóm","lại","cuối","cùng","đầu","tiên"
        };

        // Common Vietnamese function words (used by the keyword regex)
        private static readonly Regex VietnameseKeywordRegex = new Regex(
            @"\b(và|của|là|có|một|những|này|tôi|anh|em|không|rất|đã|đang|sẽ|được|với|cho|khi|nào|ở|trong|ra|nhiều|ít|vì|như|nhưng|thì|đây|kia|đó|ai|gì|đâu|sao)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Vietnamese accented characters (Latin + Vietnamese-specific diacritics)
        private static readonly Regex VietnameseAccentRegex = new Regex(
            "[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđÀÁẢÃẠĂẰẮẲẴẶÂẦẤẨẪẬÈÉẺẼẸÊỀẾỂỄỆÌÍỈĨỊÒÓỎÕỌÔỒỐỔỖỘƠỜỚỞỠỢÙÚỦŨỤƯỪỨỬỮỰỲÝỶỸỴĐ]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Vietnamese / other Latin-script language disambiguation ----

        // Vietnamese-specific characters: đ, ơ, ư and the full set of tone-marked vowels
        // đ (U+0111) and ơ (U+01A1), ư (U+01B0) are not used in other Latin-script languages in this set
        private static readonly Regex VietnameseSpecificCharRegex = new Regex(
            @"[đơưĐƠƯ]",
            RegexOptions.Compiled);

        // Characters that suggest NOT Vietnamese (shared with other Latin languages but not Vietnamese)
        private static readonly Regex NonVietnameseCharRegex = new Regex(
            @"[ñçğışřěůąłńśźżľĺŕôäåøæœ]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Vietnamese;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return VietnameseKeywordRegex.Matches(Input).Count;
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
            const double StrongSignalScore = 0.7;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int AccentCount = VietnameseAccentRegex.Matches(Input).Count;
            double AccentRatio = (double)AccentCount / LetterCount;

            // Base score driven by Vietnamese accented character ratio
            double Score = AccentRatio >= MinAccentRatio
                ? PassScore + (1 - PassScore) * (AccentRatio - MinAccentRatio) / (1 - MinAccentRatio)
                : MaxFailScore * AccentRatio / MinAccentRatio;

            bool HasVietnameseSpecific = VietnameseSpecificCharRegex.IsMatch(Input);
            bool HasNonVietnamese = NonVietnameseCharRegex.IsMatch(Input);

            // Strong Vietnamese signal: đ/ơ/ư are not used in other Latin-script languages in this set
            if (HasVietnameseSpecific)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Non-Vietnamese-specific characters without Vietnamese-specific: weak negative
            else if (HasNonVietnamese)
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            int KeywordHits = CountKeywordHits(Input);

            // One Vietnamese keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Non-Vietnamese-specific characters still cap final score below PassScore
            if (!HasVietnameseSpecific && HasNonVietnamese)
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}