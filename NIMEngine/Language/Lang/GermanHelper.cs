using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class GermanHelper : I_Language
    {
        public string Code { get; } = "de";
        public string LanguageName { get; } = "German";
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles and determiners
            "der","die","das",
            "den","dem","des",
            "ein","eine","einer","eines","einem","einen",
            "dieser","diese","dieses","diesem","diesen",
            "jeder","jede","jedes","jedem","jeden",
            "welcher","welche","welches","welchem","welchen",
            "mancher","manche","manches",
            "solcher","solche","solches",
            "alle","aller","allen","allem",
            "beide","beiden","beider",
            "dies","jene","jener","jenes","jenem","jenen",
            "kein","keine","keiner","keines","keinem","keinen",
            "einige","einigen","einiger","viel","viele","vielen",
            "wenig","wenige","mehrere",

            // Conjunctions
            "und","oder","aber","denn","sondern",
            "wenn","falls","weil","da","dass","ob",
            "obwohl","während","nachdem","bevor","sobald",
            "solange","damit","sodass","indem","entweder",
            "weder","noch","sowie","als",

            // Prepositions
            "an","auf","aus","bei","bis","durch",
            "für","gegen","hinter","in","mit","nach",
            "neben","ohne","seit","über","um","unter",
            "von","vor","zu","zwischen","ab","außer",
            "entlang","gegenüber","innerhalb","außerhalb",
            "trotz","wegen","statt","anstatt","dank",
            "zum","zur","vom","beim","ins","im","am",

            // Personal and possessive pronouns
            "ich","mich","mir",
            "du","dich","dir",
            "er","ihn","ihm",
            "sie","ihr","ihnen",
            "es","wir","uns",
            "euch","selbst","selber","man",
            "mein","meine","meiner","meines","meinem","meinen",
            "dein","deine","deiner","deines","deinem","deinen",
            "unser","unsere","unserer","unseres","unserem","unseren",
            "euer","eure","eurer","eures","eurem","euren",
            "sich","seine","seiner","seines","seinem","seinen",
            "ihre","ihrer","ihres","ihrem","ihren",

            // Interrogative and relative pronouns
            "wer","wen","wem","wessen",
            "was","wo","woher","wohin","wann","warum",
            "wieso","weshalb","wodurch","womit","wie",

            // Auxiliary and modal verbs
            "sein","bin","bist","ist","sind","seid",
            "war","warst","waren","wart","gewesen","worden","geworden",
            "haben","habe","hast","hat","habt","hatte",
            "hatten","hattet","gehabt",
            "werden","werde","wirst","wird","werdet",
            "wurde","wurden","würde","würden",
            "können","kann","kannst","könnt","konnte","konnten",
            "müssen","muss","musst","müsst","musste","mussten",
            "sollen","soll","sollst","sollt","sollte","sollten",
            "wollen","will","willst","wollt","wollte","wollten",
            "dürfen","darf","darfst","dürft","durfte","durften",
            "mögen","mag","magst","möchte","möchten",

            // Common adverbs
            "auch","nur","schon","bereits","nicht","mehr",
            "immer","oft","manchmal","selten","meistens",
            "sehr","so","dann","danach","davor",
            "nichts","nie","niemals","niemand",
            "hier","dort","jetzt","heute","morgen",
            "gestern","wieder","weiter","vielleicht",
            "eigentlich","wohl","eben","erst",
            "fast","ganz","zusammen","allerdings","jedoch",
            "deshalb","deswegen","darum","also",
            "dabei","davon","dazu","daher","darauf",
            "darüber","darin","daran","daraus",

            // Other common words
            "je","etc"
        };

        // Stop words that also exist in English or other languages.
        // Still filtered by the tokenizer, but weak evidence for detection.
        private static readonly HashSet<string> AmbiguousWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "die","den","des","du","er","es","da","wo","um","je","etc",
            "an","in","ab","bei","bis","so","also","als",
            "was","war","wart","man","will","hat","hast",
            "bin","mag","fast","dank","erst",
            "ins","im","am",
            "dies","nie","worden"
        };

        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{M}]+", RegexOptions.Compiled);

        // German umlauts and sharp s (umlauts are also used by Turkish, Swedish, etc.)
        private static readonly Regex GermanCharRegex = new Regex("[äöüß]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Letters not used in native German words.
        // Both cases are listed explicitly (no IgnoreCase) to avoid Turkish I casing issues.
        private static readonly Regex ForeignCharRegex = new Regex(
            "[çÇğĞıİşŞåÅõÕőŐűŰøØæÆñÑãÃąĄćĆęĘłŁńŃśŚźŹżŻčČďĎěĚňŇřŘšŠťŤůŮžŽ]",
            RegexOptions.Compiled);

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

            bool HasGermanChar = GermanCharRegex.IsMatch(Input);

            // Short text (item names, UI labels): one clear clue is enough
            if (WordCount <= ShortTextWords)
            {
                bool HasClue = StrongHits >= 1 || Input.IndexOf('ß') >= 0 ||
                    (HasGermanChar && !ForeignCharRegex.IsMatch(Input));

                if (HasClue)
                    return PassScore + (1 - PassScore) * StrongHits / WordCount;

                return 0.25 * WeakHits / WordCount;
            }

            double HitRatio = (StrongHits + WeakHits * 0.5) / WordCount;

            bool HasSecondClue = StrongHits >= 2 || WeakHits > 0 || HasGermanChar;

            if (StrongHits >= 1 && HasSecondClue && HitRatio >= MinHitRatio)
            {
                double Extra = (HitRatio - MinHitRatio) / (FullHitRatio - MinHitRatio);

                return PassScore + (1 - PassScore) * Math.Min(1.0, Extra);
            }

            return Math.Min(MaxFailScore, MaxFailScore * HitRatio / MinHitRatio);
        }
    }
}