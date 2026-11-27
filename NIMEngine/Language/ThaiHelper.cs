using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class ThaiHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / connectors
            "และ","หรือ","แต่","เพราะ","ถ้า","เมื่อ","ขณะ","ทั้ง","ทั้งที่",
            "เพราะฉะนั้น","ดังนั้น","เพราะว่า","แม้ว่า","ถึงแม้","อย่าง","เช่น",
            "คือ","ว่า","ซึ่ง","อัน","ที่",

            // Particles / question words / negation
            "ไม่","ใช่","ใช่ไหม","หรือเปล่า","อะไร","ใคร","ที่ไหน","เมื่อไหร่",
            "ทำไม","อย่างไร","เท่าไหร่","กี่","ไหน","นี่","นั้น","โน่น","นี้","นู้น",
            "ไหม","มั้ย","นะ","สิ","ล่ะ","เหอะ","เถอะ","จ้ะ","ค่ะ","ครับ","คะ","ค่า",

            // Personal and reflexive pronouns
            "ฉัน","ผม","ดิฉัน","หนู","เรา","พวกเรา","คุณ","ท่าน","พวกคุณ",
            "เขา","เธอ","มัน","พวกเขา","พวกมัน","ตัวเอง","ตนเอง","กัน","กันเอง",

            // Prepositions
            "ใน","บน","ใต้","เหนือ","หน้า","หลัง","ข้าง","ระหว่าง","ใกล้","ไกล",
            "กับ","ต่อ","จาก","ถึง","โดย","ด้วย","เพื่อ","สำหรับ","เกี่ยวกับ","ตาม",
            "ก่อน","หลัง","เมื่อ","จน","จนกระทั่ง","ตั้งแต่","ตลอด","เว้น","ยกเว้น",

            // Demonstrative pronouns and adjectives
            "นี้","นั้น","โน่น","นี่","นั่น","โน้น","เช่นนี้","เช่นนั้น","อย่างนี้","อย่างนั้น",

            // Auxiliary / modal / copula words
            "เป็น","คือ","อยู่","มี","ได้","ต้อง","ควร","อาจ","สามารถ","กำลัง","จะ",
            "เคย","ไม่เคย","แล้ว","ยัง","เพิ่ง","เคย","ถูก","ให้",

            // Negative and indefinite words
            "ไม่","ไม่มี","ไม่เคย","ไม่ใช่","ไม่ต้อง","ไม่ควร","ห้าม","อย่า",
            "ใคร","อะไร","ที่ไหน","บาง","บ้าง","ทุก","ทุกคน","ทุกสิ่ง","อื่น","อื่นๆ",

            // Common adverbs
            "มาก","น้อย","ที่สุด","กว่า","เกิน","เพียง","แค่","เท่านั้น","เท่า",
            "ตอนนี้","เดี๋ยวนี้","วันนี้","เมื่อวาน","พรุ่งนี้","ตลอด","เสมอ",
            "บ่อย","บางครั้ง","บางที","อาจจะ","แน่นอน","จริงๆ","จริง","ค่อย","เกือบ",

            // Discourse markers
            "ดังนั้น","เพราะฉะนั้น","อย่างไรก็ตาม","แต่ถึงอย่างนั้น","ยกตัวอย่าง",
            "กล่าวคือ","สรุป","สุดท้าย","นอกจากนี้","อีกทั้ง","อนึ่ง"
        };

        // Common Thai function words / particles (used by the keyword regex)
        private static readonly Regex ThaiKeywordRegex = new Regex(
            @"(และ|ของ|เป็น|มี|ไม่|ได้|ว่า|ใน|ที่|กับ|ก็|จะ|มา|ไป|คน|อะไร|นี่|นั้น)",
            RegexOptions.Compiled);

        // Thai Unicode block: U+0E00 – U+0E7F
        private static readonly Regex ThaiCharRegex = new Regex(
            @"[\u0E00-\u0E7F]",
            RegexOptions.Compiled);

        // Letters (Thai + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Thai;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return ThaiKeywordRegex.Matches(Input).Count;
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
            const double MinThaiRatio = 0.3;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int ThaiCharCount = ThaiCharRegex.Matches(Input).Count;
            double ThaiCharRatio = (double)ThaiCharCount / LetterCount;

            // Base score driven by Thai-script character ratio
            double Score = ThaiCharRatio >= MinThaiRatio
                ? PassScore + (1 - PassScore) * (ThaiCharRatio - MinThaiRatio) / (1 - MinThaiRatio)
                : MaxFailScore * ThaiCharRatio / MinThaiRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One Thai keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}