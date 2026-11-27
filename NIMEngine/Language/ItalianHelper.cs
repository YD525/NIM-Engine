using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class ItalianHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles
            "il","lo","la","i","gli","le","un","uno","una",

            // Conjunctions
            "e","ed","o","od","ma","però","che","se","perché","poiché","siccome",
            "quando","mentre","come","anche","oppure","quindi","dunque",
            "allora","inoltre","tuttavia","perciò","cioè",

            // Prepositions / articulated prepositions
            "di","a","da","in","con","su","per","tra","fra","del","dello","della",
            "dei","degli","delle","al","allo","alla","ai","agli","alle","dal",
            "dallo","dalla","dai","dagli","dalle","nel","nello","nella","nei",
            "negli","nelle","sul","sullo","sulla","sui","sugli","sulle","col",
            "coi","colla","senza","sotto","sopra","dentro","fuori","verso","contro",
            "secondo","durante","prima","dopo","accanto","vicino","lontano",

            // Personal and reflexive pronouns
            "io","mi","me","tu","ti","te","lui","lei","esso","essa","noi","ci",
            "voi","vi","loro","essi","esse","si","sé",
            "mio","mia","miei","mie","tuo","tua","tuoi","tue","suo","sua","suoi",
            "sue","nostro","nostra","nostri","nostre","vostro","vostra","vostri",
            "vostre",

            // Demonstrative pronouns and adjectives
            "questo","questa","questi","queste","quello","quella","quelli","quelle",
            "codesto","codesta","codesti","codeste","ciò","qui","qua","lì","là",
            "costui","costei","costoro","colui","colei","coloro",

            // Interrogative and relative words
            "chi","cui","quale","quali","quanto","quanta","quanti","quante",
            "dove","affinché","benchè","benché","purché",

            // Negative and indefinite words
            "non","no","niente","nulla","nessuno","nessuna","alcuno","alcuna",
            "qualche","qualcosa","qualcuno","ogni","tutti","tutte","tutto","tutta",
            "ciascuno","ciascuna","altro","altra","altri","altre","certo","certa",

            // Auxiliary / modal / copula words
            "essere","sono","sei","è","siamo","siete","ero","eri","era","erano",
            "stato","stata","stati","state","avere","ho","hai","ha","abbiamo",
            "avete","hanno","avevo","avevi","aveva","avevano","avuto","avuta",
            "potere","posso","puoi","può","possiamo","potete","possono","dovere",
            "devo","devi","deve","dobbiamo","dovete","devono","volere","voglio",
            "vuoi","vuole","vogliamo","volete","vogliono",

            // Common adverbs
            "molto","molta","molti","molte","poco","poca","pochi","poche",
            "più","meno","tanto","tanta","tanti","tante","troppo","troppa",
            "abbastanza","così","proprio","davvero","veramente","certamente",
            "sicuramente","forse","probabilmente","già","ancora","sempre",
            "mai","spesso","ora","adesso","poi","oggi","ieri","domani",
            "stamattina","stasera",

            // Discourse markers
            "infatti","insomma","comunque","altrimenti","piuttosto",
            "addirittura","appunto","dopotutto"
        };

        private static readonly Regex ItalianKeywordRegex = new Regex(
            @"\b(il|la|e|di|che|per|un|una|non|sono|da|con)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ItalianAccentRegex = new Regex(
            "[àèéìíòóùú]",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Italian;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return ItalianKeywordRegex.Matches(Input).Count;
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
            const double MinAccentRatio = 0.005;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int AccentCount = ItalianAccentRegex.Matches(Input).Count;
            double AccentRatio = (double)AccentCount / LetterCount;

            double Score = AccentRatio >= MinAccentRatio
                ? PassScore + (1 - PassScore) * (AccentRatio - MinAccentRatio) / (1 - MinAccentRatio)
                : MaxFailScore * AccentRatio / MinAccentRatio;

            int KeywordHits = CountKeywordHits(Input);

            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}