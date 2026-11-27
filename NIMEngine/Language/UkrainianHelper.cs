using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class UkrainianHelper : I_Language
    {
        public string Code { get; } = "uk";
        public string LanguageName { get; } = "Ukrainian";
        public double PassScore { get; set; } = 0.5;
        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions
            "і","й","та","а","але","або","чи","бо","що","щоб","щоби","як","якщо","коли","поки",
            "доки","хоча","хоч","проте","однак","зате","тому","також","теж","немов","наче","ніби",
            "неначе","мов","отже",

            // Prepositions
            "в","у","уві","ві","на","з","із","зі","зо","до","від","од","по","під","піді","над",
            "при","про","для","без","через","між","поміж","перед","переді","після","біля","коло",
            "серед","крім","окрім","замість","заради","задля","за","о","об","к","ради","щодо",
            "навколо","довкола","вздовж","протягом","з-за","з-під",

            // Particles
            "не","ні","ж","же","б","би","ось","от","он","адже","лише","лиш","тільки","навіть",
            "вже","уже","ще","хай","нехай","хіба","невже","саме","так","таки","все-таки",

            // Personal and reflexive pronouns
            "я","мене","мені","мною",
            "ти","тебе","тобі","тобою",
            "він","його","нього","йому","ним","нім","ньому",
            "вона","її","неї","їй","ній","нею",
            "воно",
            "ми","нас","нам","нами",
            "ви","вас","вам","вами",
            "вони","їх","них","їм","ними",
            "себе","собі","собою",

            // Possessive pronouns
            "мій","моя","моє","мої","мого","моєї","моєму","моїй","моїм","моїх","моїми","мою","моєю",
            "твій","твоя","твоє","твої","твого","твоєї","твоєму","твоїй","твоїм","твоїх","твоїми","твою","твоєю",
            "наш","наша","наше","наші","нашого","нашої","нашому","нашій","нашим","наших","нашими","нашу","нашою",
            "ваш","ваша","ваше","ваші","вашого","вашої","вашому","вашій","вашим","ваших","вашими","вашу","вашою",
            "свій","своя","своє","свої","свого","своєї","своєму","своїй","своїм","своїх","своїми","свою","своєю",
            "їхній","їхня","їхнє","їхні",

            // Demonstrative and determinative pronouns
            "це","цей","ця","ці","цього","цієї","цьому","цій","цим","цих","цими","цю","цією",
            "той","те","ті","того","тієї","тій","тим","тих","тими","ту","тією",
            "такий","така","таке","такі",
            "весь","увесь","вся","все","всі","усе","усі","уся","всього","всієї","всьому","всім","всіх","всіма","всю",
            "кожен","кожний","кожна","кожне","кожні",
            "сам","сама","самі",
            "інший","інша","інше","інші",

            // Interrogative and relative pronouns
            "хто","кого","кому","ким","чого","чому","чим",
            "який","яка","яке","які","якого","якої","якому","якій","яким","яких","якими","яку","якою",
            "чий","чия","чиє","чиї",
            "де","куди","звідки","навіщо","скільки",

            // Negative and indefinite words
            "ніхто","ніщо","нічого","ніколи","ніде","нікуди","жоден","жодна","жодне","жодні",
            "хтось","щось","якийсь","десь","кудись","колись",
            "будь","небудь","будь-який","будь-яка","будь-яке","будь-які","будь-хто","будь-що",
            "хто-небудь","що-небудь",

            // Auxiliary and modal words
            "є","бути","був","була","було","були",
            "буду","будеш","буде","будемо","будете","будуть",
            "можу","можеш","може","можемо","можете","можуть","міг","могла","могли",
            "можна","треба","потрібно","немає","нема",

            // Common adverbs
            "дуже","там","тут","сюди","туди","звідси","звідти",
            "тоді","тепер","зараз","потім","завжди","іноді","інколи","часто",
            "знову","майже","зовсім","більш","більше","менш","менше","занадто","надто",
            "сьогодні","завтра","вчора","учора"
        };

        // Words that also exist in Russian or other Cyrillic languages.
        // Still filtered by the tokenizer, but never used as evidence of Ukrainian.
        private static readonly HashSet<string> SharedWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "а","та","чи","бо","як","тому","мов",
            "в","у","на","з","зо","до","од","по","над","при","про","для","без","через","перед",
            "коло","за","о","об","к","ради",
            "не","ж","же","б","би","от","он","хай","саме","так","таки",
            "я","мене","тебе","ти","нас","нам","нами","ми","ви","вас","вам","вами",
            "них","ним","ними","нею","себе","собою","мною","тобою",
            "моя","мою","твоя","твою","наш","наша","наше","нашу","нашим","наших","нашими",
            "ваш","ваша","ваше","вашу","вашим","ваших","вашими","своя","свою",
            "той","те","того","тим","тих","тими","ту",
            "вся","все","всю","весь","сам","сама",
            "кого","кому","чого","чому","чим","ким",
            "яка","яку","де",
            "буду","можу","може","можна","треба","нема",
            "там","тут","часто","завтра","будь",
            "уже","будете","могла","могли","можете","ось","лиш","доки","зараз","чий"
        };

        // Words may contain an apostrophe or hyphen: м'ята, п’ять, будь-який
        private static readonly Regex WordRegex = new Regex(
            @"[\p{L}\p{M}]+(?:['’ʼ-][\p{L}\p{M}]+)*",
            RegexOptions.Compiled);

        // Cyrillic characters
        private static readonly Regex CyrillicCharRegex = new Regex(@"[\u0400-\u04FF]", RegexOptions.Compiled);

        // Letters not used in Russian: Ґ Є І Ї
        private static readonly Regex UkrainianCharRegex = new Regex("[ґҐєЄіІїЇ]", RegexOptions.Compiled);

        // Apostrophe before an iotated vowel: п'ять, об'єкт, з’їзд
        private static readonly Regex UkrainianApostropheRegex = new Regex(
            @"[бпвмфрдзтнк]['’ʼ][яюєї]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Letters not used in Ukrainian
        // Russian: ы э ъ ё / Belarusian: ў / Serbian, Macedonian: ј љ њ ћ ђ џ ѓ ќ ѕ
        // Kazakh and others: ә ғ қ ң ө ұ ү һ
        private static readonly Regex ForeignCharRegex = new Regex(
            "[ыЫэЭъЪёЁўЎјЈљЉњЊћЋђЂџЏѓЃќЌѕЅәӘғҒқҚңҢөӨұҰүҮһҺ]",
            RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static bool IsUniqueStopWord(string Word)
        {
            return StopWords.Contains(Word) && !SharedWords.Contains(Word);
        }

        // A word is evidence if it has a Ukrainian letter, a Ukrainian apostrophe,
        // or is a stop word that Russian does not use
        private static bool IsEvidence(string Word)
        {
            return UkrainianCharRegex.IsMatch(Word) ||
                UkrainianApostropheRegex.IsMatch(Word) ||
                IsUniqueStopWord(Word);
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
            const double MinCyrillicRatio = 0.1;
            const double MaxFailScore = 0.49;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int CyrillicCount = CyrillicCharRegex.Matches(Input).Count;

            if (CyrillicCount == 0 || ForeignCharRegex.IsMatch(Input))
                return 0;

            List<string> Words = WordRegex.Matches(Input).Cast<Match>().Select(Word => Word.Value).ToList();

            if (Words.Count == 0)
                return 0;

            int EvidenceCount = Words.Count(IsEvidence);

            // Cyrillic text without any evidence is not enough (it may be Russian)
            if (EvidenceCount == 0)
            {
                int SharedHits = Words.Count(Word => StopWords.Contains(Word));

                return 0.25 * SharedHits / Words.Count;
            }

            // Spaces, digits and punctuation are not counted
            int LetterCount = Input.Count(char.IsLetter);

            double CyrillicRatio = (double)CyrillicCount / Math.Max(LetterCount, CyrillicCount);

            if (CyrillicRatio < MinCyrillicRatio && !Words.Any(IsUniqueStopWord))
                return MaxFailScore * CyrillicRatio / MinCyrillicRatio;

            return PassScore + (1 - PassScore) * EvidenceCount / Words.Count;
        }
    }
}