using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class PortugueseHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Articles
            "o","a","os","as","um","uma","uns","umas",

            // Conjunctions
            "e","ou","mas","porém","contudo","todavia","entretanto","porque","pois",
            "que","se","como","quando","enquanto","embora","apesar","caso","logo",
            "portanto","assim","também","ainda","já","até","nem","sequer",

            // Prepositions / contractions
            "de","do","da","dos","das","em","no","na","nos","nas","por","pelo","pela",
            "pelos","pelas","para","pra","com","sem","sob","sobre","entre","contra",
            "desde","até","após","ante","perante","trás","além","acima","abaixo",
            "dentro","fora","diante","através","durante","mediante","conforme","segundo",

            // Personal and reflexive pronouns
            "eu","me","mim","tu","te","ti","você","vocês","ele","ela","eles","elas",
            "nós","nos","vós","vos","lhe","lhes","se","si","consigo",
            "meu","minha","meus","minhas","teu","tua","teus","tuas",
            "seu","sua","seus","suas","nosso","nossa","nossos","nossas",
            "vosso","vossa","vossos","vossas",

            // Demonstrative pronouns and adjectives
            "este","esta","estes","estas","esse","essa","esses","essas",
            "aquele","aquela","aqueles","aquelas","isto","isso","aquilo",
            "aqui","ali","lá","acolá","cá",

            // Interrogative and relative words
            "quem","qual","quais","quanto","quanta","quantos","quantas",
            "onde","aonde","porque","porquê","como","quando","cujo","cuja",

            // Negative and indefinite words
            "não","nunca","jamais","nada","ninguém","nenhum","nenhuma",
            "algum","alguma","alguns","algumas","alguém","algo","cada","todo",
            "toda","todos","todas","outro","outra","outros","outras","qualquer",

            // Auxiliary / modal / copula words
            "ser","sou","és","é","somos","são","era","eram","foi","foram",
            "sido","estar","estou","estás","está","estamos","estão","estava",
            "estavam","esteve","estiveram","estado","ter","tenho","tens","tem",
            "temos","têm","tinha","tinham","teve","tiveram","tido","haver","há",
            "houve","poder","posso","podes","pode","podemos","podem","podia",
            "dever","devo","deves","deve","devemos","devem","deveria",
            "querer","quero","queres","quer","queremos","querem","queria",

            // Common adverbs
            "muito","muita","muitos","muitas","pouco","pouca","poucos","poucas",
            "mais","menos","tanto","tanta","tanto","tão","demais","bastante",
            "bem","mal","melhor","pior","agora","hoje","ontem","amanhã",
            "sempre","nunca","jamais","às","vezes","depois","antes","logo",
            "cedo","tarde","aqui","ali","lá","cá","assim","talvez","certamente",

            // Discourse markers
            "enfim","afinal","aliás","inclusive","portanto","então","porém",
            "contudo","todavia","entretanto","ou seja","isto é","por exemplo"
        };

        // Common Portuguese function words (used by the keyword regex)
        private static readonly Regex PortugueseKeywordRegex = new Regex(
            @"\b(o|a|os|as|de|do|da|dos|das|que|e|em|por|para|com|como|mas|se|foi|sou|está|estão|ser|estar|ter|tem|não|sim|eu|você|ele|ela|nós|eles|elas|um|uma|uns|umas|há|vai|vou|fui|tive|também|muito|mais|menos|vós|lhe|meu|minha|teu|tua|seu|sua|nos|vos|este|esse|aquele|aquela|onde|quando|porque|qual|sem|além|até|entre|sobre|contra)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Portuguese special characters with diacritics (accents and cedilla)
        private static readonly Regex PortugueseSpecialCharRegex = new Regex(
            "[áàâãäéèêëíìîïóòôõöúùûüçÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇ]",
            RegexOptions.Compiled);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Brazilian / European Portuguese disambiguation ----

        // Brazilian Portuguese specific markers (você/vocês, gerund -ndo, te/ti, etc.)
        private static readonly Regex BrazilianMarkerRegex = new Regex(
            @"\b(você|vocês|a gente|gerúndio)\b|\b\w+ndo\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // European Portuguese specific markers (tu/vós with verb agreement, enclitic pronouns, etc.)
        private static readonly Regex EuropeanMarkerRegex = new Regex(
            @"\b(tu|vós|convosco)\b|\b\w+-me\b|\b\w+-te\b|\b\w+-lhe\b|\b\w+-nos\b|\b\w+-vos\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public int GetLanguage()
        {
            return (int)Languages.Portuguese;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return PortugueseKeywordRegex.Matches(Input).Count;
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
            const double MinSpecialCharRatio = 0.01;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double VariantSignalScore = 0.65;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int SpecialCharCount = PortugueseSpecialCharRegex.Matches(Input).Count;
            double SpecialCharRatio = (double)SpecialCharCount / LetterCount;

            // Base score driven by Portuguese diacritic ratio
            double Score = SpecialCharRatio >= MinSpecialCharRatio
                ? PassScore + (1 - PassScore) * (SpecialCharRatio - MinSpecialCharRatio) / (1 - MinSpecialCharRatio)
                : MaxFailScore * SpecialCharRatio / MinSpecialCharRatio;

            // Variant signal: Brazilian or European markers boost confidence
            if (BrazilianMarkerRegex.IsMatch(Input) || EuropeanMarkerRegex.IsMatch(Input))
                Score = Math.Max(Score, VariantSignalScore);

            int KeywordHits = CountKeywordHits(Input);

            // One Portuguese keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}