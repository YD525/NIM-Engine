using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class PolishHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions / particles
            "i","a","ale","lub","czy","że","żeby","gdy","kiedy","bo","ponieważ",
            "więc","zatem","jednak","lecz","albo","bądź","jeśli","jeżeli","gdyby",
            "choć","chociaż","mimo","bowiem","dlatego","gdzie","dokąd","odkąd",

            // Prepositions
            "w","we","na","z","ze","do","od","po","przed","za","nad","pod","między",
            "bez","przez","przy","o","ob","u","ku","dla","według","wobec","wśród",
            "obok","koło","wokół","naprzeciw","wzdłuż","wewnątrz","zewnątrz","podczas",

            // Personal and reflexive pronouns
            "ja","mnie","mi","mną","ty","ciebie","ci","tobą","on","jego","jemu","nim",
            "ona","jej","nią","ono","my","nas","nam","nami","wy","was","wam","wami",
            "oni","one","ich","im","nimi","się","siebie","sobie","sobą",

            // Possessive pronouns
            "mój","moja","moje","mojego","mojej","mojemu","moim","moich","moimi",
            "twój","twoja","twoje","twojego","twojej","twojemu","twoim","twoich","twoimi",
            "jego","jej","nasz","nasza","nasze","naszego","naszej","naszemu","naszym",
            "wasz","wasza","wasze","waszego","waszej","waszemu","waszym",
            "swój","swoja","swoje","swojego","swojej","swojemu","swoim","swoich","swoimi",

            // Demonstrative pronouns and adjectives
            "ten","ta","to","ci","te","tego","tej","temu","tym","tych","tymi",
            "tamten","tamta","tamto","tamci","tamte","taki","taka","takie","takiego",

            // Interrogative and relative words
            "kto","co","który","która","które","którego","której","któremu","którym",
            "których","którymi","czyj","jaki","jaka","jakie","jak","gdzie","kiedy",
            "dlaczego","jak","skąd","dokąd","ile","dopóki","jeśli","czy",

            // Negative and indefinite words
            "nie","nic","nikt","nigdy","nigdzie","żaden","żadna","żadne",
            "coś","ktoś","kiedyś","gdzieś","jakiś","jakaś","jakieś",
            "każdy","każda","każde","wszyscy","wszystkie","wszystko","inny","inna","inne",

            // Auxiliary / modal / copula words
            "być","jestem","jesteś","jest","jesteśmy","jesteście","są","byłem","byłam",
            "byli","były","będę","będzie","będziemy","będą","mieć","mam","masz","ma",
            "mamy","macie","mają","miałem","miała","mieli","miały","móc","mogę","możesz",
            "może","możemy","możecie","mogą","musieć","muszę","musisz","musi","musimy",
            "musicie","muszą","chcieć","chcę","chcesz","chce","chcemy","chcecie","chcą",

            // Common adverbs
            "bardzo","mocno","dużo","mało","więcej","mniej","najwięcej","najmniej",
            "teraz","obecnie","dziś","wczoraj","jutro","rano","wieczorem","nocą",
            "zawsze","nigdy","często","czasem","rzadko","zwykle","już","jeszcze",
            "tu","tam","tutaj","potem","wkrótce","późno","dobrze","źle",

            // Discourse markers
            "więc","zatem","także","mianowicie","na przykład","zwłaszcza",
            "przede wszystkim","wreszcie","najpierw","oprócz tego","przeciwnie",
            "innymi słowy","krótko mówiąc","ogólnie"
        };

        // Common Polish function words (used by the keyword regex)
        private static readonly Regex PolishKeywordRegex = new Regex(
            @"\b(że|jest|nie|tak|jak|co|czy|go|za|po|się|dla|ten|tego|być|był|była|było|jestem|jesteś|są|mamy|macie|oni|one|który|która|które|którego|którą|więc|bardzo|jeszcze|może|muszę|chcę)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Polish-specific accented characters: ą ć ę ł ń ó ś ź ż and uppercase variants
        private static readonly Regex PolishAccentCharRegex = new Regex(
            "[ąćęłńóśźżĄĆĘŁŃÓŚŹŻ]",
            RegexOptions.Compiled);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Polish / Czech / Slovak disambiguation ----

        // Polish-specific characters not used in Czech/Slovak: ą, ł, ń, ś, ź, ż
        // (ę and ó also in Czech, ć and ń in some Slavic but not standard Czech/Slovak)
        private static readonly Regex PolishSpecificCharRegex = new Regex(
            @"[ąłńśźżĄŁŃŚŹŻ]",
            RegexOptions.Compiled);

        // Czech-specific characters not used in Polish: ř, ě, ů
        private static readonly Regex CzechSpecificCharRegex = new Regex(
            @"[řěůŘĚŮ]",
            RegexOptions.Compiled);

        // Slovak-specific characters not used in Polish: ľ, ĺ, ŕ, ô, ä
        private static readonly Regex SlovakSpecificCharRegex = new Regex(
            @"[ľĺŕôäĽĹŔÔÄ]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Polish;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return PolishKeywordRegex.Matches(Input).Count;
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
            const double MinAccentRatio = 0.02;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double StrongSignalScore = 0.7;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int AccentCount = PolishAccentCharRegex.Matches(Input).Count;
            double AccentRatio = (double)AccentCount / LetterCount;

            // Base score driven by Polish accented character ratio
            double Score = AccentRatio >= MinAccentRatio
                ? PassScore + (1 - PassScore) * (AccentRatio - MinAccentRatio) / (1 - MinAccentRatio)
                : MaxFailScore * AccentRatio / MinAccentRatio;

            bool HasPolishSpecific = PolishSpecificCharRegex.IsMatch(Input);
            bool HasCzechSpecific = CzechSpecificCharRegex.IsMatch(Input);
            bool HasSlovakSpecific = SlovakSpecificCharRegex.IsMatch(Input);

            // Strong Polish signal: ą/ł/ń/ś/ź/ż not used in Czech/Slovak
            if (HasPolishSpecific)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Czech/Slovak-specific characters without Polish-specific: likely not Polish
            else if (HasCzechSpecific || HasSlovakSpecific)
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            int KeywordHits = CountKeywordHits(Input);

            // One Polish keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Czech/Slovak-specific characters still cap final score below PassScore
            if (!HasPolishSpecific && (HasCzechSpecific || HasSlovakSpecific))
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}