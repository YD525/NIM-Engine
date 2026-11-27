using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class SpanishHelper : I_Language
    {
        public string Code { get; } = "es";
        public string LanguageName { get; } = "Spanish";
        public double PassScore { get; set; } = 0.5;
        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles
            "el","la","los","las","un","una","unos","unas","lo",

            // Conjunctions
            "y","e","o","u","pero","sino","mas","porque","pues","ya","que","si",
            "como","cuando","mientras","aunque","aunque","conque","luego","entonces",
            "además","también","tampoco","incluso","hasta","ni","siquiera",

            // Prepositions
            "de","del","a","al","en","con","por","para","sin","sobre","bajo",
            "entre","hacia","hasta","desde","durante","mediante","según","contra",
            "ante","tras","según","sobre","acerca","alrededor","durante","excepto",
            "salvo","incluso","menos",

            // Personal and reflexive pronouns
            "yo","me","mí","tú","te","ti","usted","ustedes","él","ella","ellos",
            "ellas","nosotros","nosotras","nos","vosotros","vosotras","os","les",
            "se","sí","conmigo","contigo","consigo",
            "mi","mis","mío","mía","míos","mías","tu","tus","tuyo","tuya","tuyos","tuyas",
            "su","sus","suyo","suya","suyos","suyas","nuestro","nuestra","nuestros","nuestras",
            "vuestro","vuestra","vuestros","vuestras",

            // Demonstrative pronouns and adjectives
            "este","esta","estos","estas","ese","esa","esos","esas",
            "aquel","aquella","aquellos","aquellas","esto","eso","aquello",
            "aquí","ahí","allí","acá","allá",

            // Interrogative and relative words
            "quién","quiénes","qué","cuál","cuáles","cuánto","cuánta","cuántos","cuántas",
            "dónde","cuándo","cómo","por qué","para qué","cuyo","cuya","cuyos","cuyas",

            // Negative and indefinite words
            "no","nunca","jamás","nada","nadie","ningún","ninguno","ninguna",
            "algún","alguno","alguna","algunos","algunas","alguien","algo","cada",
            "todo","toda","todos","todas","otro","otra","otros","otras","cualquiera",

            // Auxiliary / modal / copula words
            "ser","soy","eres","es","somos","son","era","eran","fue","fueron",
            "sido","estar","estoy","estás","está","estamos","están","estaba",
            "estaban","estuvo","estuvieron","estado","tener","tengo","tienes","tiene",
            "tenemos","tienen","tenía","tenían","tuvo","tuvieron","tenido",
            "haber","he","has","ha","hemos","han","había","habían","hubo",
            "habido","poder","puedo","puedes","puede","podemos","pueden","podía",
            "deber","debo","debes","debe","debemos","deben","debería",
            "querer","quiero","quieres","quiere","queremos","quieren","quería",

            // Common adverbs
            "muy","mucho","mucha","muchos","muchas","poco","poca","pocos","pocas",
            "más","menos","tanto","tanta","tantos","tantas","tan","demasiado",
            "bien","mal","mejor","peor","ahora","hoy","ayer","mañana",
            "siempre","nunca","jamás","a veces","después","antes","luego",
            "temprano","tarde","aquí","ahí","allí","así","tal vez","quizás",
            "ciertamente","realmente","verdaderamente",

            // Discourse markers
            "en fin","por fin","además","inclusive","entonces","por lo tanto",
            "sin embargo","no obstante","es decir","por ejemplo"
        };

        // Common Spanish function words (used by the keyword regex)
        private static readonly Regex SpanishKeywordRegex = new Regex(
            @"\b(el|la|los|las|un|una|unos|unas|y|de|que|en|no|soy|eres|es|somos|son|con|por|para|se|me|te|le|lo|nos|os|les|más|pero|sí|porque|como|cuando|donde|qué|quién|este|ese|aquel)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Spanish-specific accented characters and ñ, ü
        private static readonly Regex SpanishAccentCharRegex = new Regex(
            "[áéíóúñüÁÉÍÓÚÑÜ]",
            RegexOptions.Compiled);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return SpanishKeywordRegex.Matches(Input).Count;
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

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int AccentCount = SpanishAccentCharRegex.Matches(Input).Count;
            double AccentRatio = (double)AccentCount / LetterCount;

            // Base score driven by Spanish accented character ratio
            double Score = AccentRatio >= MinAccentRatio
                ? PassScore + (1 - PassScore) * (AccentRatio - MinAccentRatio) / (1 - MinAccentRatio)
                : MaxFailScore * AccentRatio / MinAccentRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One Spanish keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}