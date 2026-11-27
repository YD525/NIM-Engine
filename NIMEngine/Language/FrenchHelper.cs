using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class FrenchHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles and determiners
            "le","la","les","un","une","des","du","de","au","aux",
            "ce","cet","cette","ces",
            "chaque","quelque","quelques","plusieurs",
            "tout","toute","tous","toutes",
            "aucun","aucune","autre","autres",

            // Conjunctions
            "et","ou","mais","donc","car","ni","que","si",
            "comme","lorsque","quand","puisque","quoique","parce",

            // Prepositions
            "à","dans","sur","sous","avec","sans","pour","par","chez",
            "vers","entre","contre","depuis","pendant","avant","après",
            "devant","derrière","en",

            // Personal and reflexive pronouns
            "je","tu","il","elle","on","nous","vous","ils","elles",
            "me","te","se","moi","toi","lui","leur","eux","y",

            // Possessive determiners
            "mon","ma","mes","ton","ta","tes","son","sa","ses",
            "notre","nos","votre","vos","leurs",

            // Interrogative and relative pronouns
            "qui","quoi","où","dont","comment","pourquoi","combien",
            "quel","quelle","quels","quelles","lequel","laquelle",

            // Demonstrative pronouns
            "ça","cela","ceci","celui","celle","ceux","celles",

            // Auxiliary and modal verbs
            "être","suis","es","est","sommes","êtes","sont",
            "étais","était","étaient","été","sera","serait",
            "avoir","ai","as","avons","avez","ont",
            "avais","avait","avaient","eu","aura","aurait",
            "fait","peux","peut","pouvons","pouvez","peuvent",
            "dois","doit","devons","devez","doivent",
            "veux","veut","voulons","voulez","veulent",

            // Negations
            "ne","pas","non","jamais","rien","personne",

            // Common adverbs
            "très","trop","plus","moins","aussi","encore","toujours","souvent",
            "déjà","bien","ici","là","alors","ainsi","maintenant",
            "aujourd","hui","demain","hier",

            // Elided forms (the apostrophe splits them into separate words)
            "c","d","j","l","m","n","s","t","qu"
        };

        // Stop words that also exist in English, German or other languages.
        // Still filtered by the tokenizer, but weak evidence for detection.
        private static readonly HashSet<string> AmbiguousWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "la","le","un","des","du","de","au","en","on","il","tu","me","te","se",
            "ma","ta","sa","mon","ton","son","ses","mes","nos",
            "ou","mais","car","ni","que","si","par","pour","sur","entre","comment",
            "tout","plus","sans","chez","pas","non","ne","y",
            "es","est","et","ai","as","eu","ont","fait","bien",
            "personne","encore","hier",
            "c","d","j","l","m","n","s","t"
        };

        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{M}]+", RegexOptions.Compiled);

        // French accented characters (ä ö ü are left to German)
        private static readonly Regex FrenchCharRegex = new Regex(
            "[éèêëàâùûîïôçœ]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Letters that indicate another language.
        // Both cases are listed explicitly (no IgnoreCase) to avoid Turkish I casing issues.
        private static readonly Regex ForeignCharRegex = new Regex(
            "[äÄöÖüÜßáÁíÍóÓúÚìÌòÒñÑãÃõÕåÅøØğĞıİşŞąĄćĆęĘłŁńŃśŚźŹżŻčČďĎěĚňŇřŘšŠťŤůŮžŽőŐűŰ]",
            RegexOptions.Compiled);

        // Elision such as l'épée, d'or, j'ai
        private static readonly Regex ElisionRegex = new Regex(
            @"\b(?:l|d|j|qu|n|c|m|t|s)['’](?=[aeiouyhàâéèêîôœ])",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public float Version { get; set; } = 1.1f;

        public int GetLanguage()
        {
            return (int)Languages.French;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static void CountHits(string Input, out int WordCount, out int StrongHits, out int WeakHits)
        {
            WordCount = 0;
            StrongHits = 0;
            WeakHits = 0;

            foreach (Match Word in WordRegex.Matches(Input))
            {
                WordCount++;

                if (!StopWords.Contains(Word.Value))
                    continue;

                if (AmbiguousWords.Contains(Word.Value))
                    WeakHits++;
                else
                    StrongHits++;
            }
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
            const int ShortTextWords = 3;
            const double MinHitRatio = 0.15;
            const double FullHitRatio = 0.5;
            const double MaxFailScore = 0.49;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            CountHits(Input, out int WordCount, out int StrongHits, out int WeakHits);

            if (WordCount == 0)
                return 0;

            bool HasFrenchChar = FrenchCharRegex.IsMatch(Input);
            bool HasElision = ElisionRegex.IsMatch(Input);

            // Short text (item names, UI labels): one clear clue is enough
            if (WordCount <= ShortTextWords)
            {
                bool HasClue = StrongHits >= 1 || HasElision ||
                    (HasFrenchChar && !ForeignCharRegex.IsMatch(Input));

                if (HasClue)
                    return PassScore + (1 - PassScore) * StrongHits / WordCount;

                return 0.25 * WeakHits / WordCount;
            }

            // An elision counts as one strong hit
            if (HasElision)
                StrongHits++;

            double HitRatio = Math.Min(1.0, (StrongHits + WeakHits * 0.5) / WordCount);

            bool HasSecondClue = StrongHits >= 2 || WeakHits > 0 || HasFrenchChar;

            if (StrongHits >= 1 && HasSecondClue && HitRatio >= MinHitRatio)
            {
                double Extra = (HitRatio - MinHitRatio) / (FullHitRatio - MinHitRatio);

                return PassScore + (1 - PassScore) * Math.Min(1.0, Extra);
            }

            return Math.Min(MaxFailScore, MaxFailScore * HitRatio / MinHitRatio);
        }
    }
}