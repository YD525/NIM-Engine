using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class CzechHelper : I_Language
    {
        public string Code { get; } = "cs";
        public string LanguageName { get; } = "Czech";
        public double PassScore { get; set; } = 0.5;

        public float Version { get; set; } = 1.1f;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Personal and reflexive pronouns
            "já","ty","on","ona","ono","my","vy","oni","ony","ona",
            "mě","mne","mi","tě","tebe","ti","ho","jej","mu","ji","jí",
            "nás","vás","nich","ním","ními","jimi","sebe","si","se",

            // Possessive pronouns
            "můj","moje","můj","mého","mému","mým","mém","mou","moji","mých","mými",
            "tvůj","tvoje","tvého","tvému","tvým","tvém","tou","tvoji","tvých","tvými",
            "jeho","její","jejích","jejím","jející",
            "náš","naše","našeho","našemu","naším","našem","naši","našich","našimi",
            "váš","vaše","vašeho","vašemu","vaším","vašem","vaši","vašich","vašimi",
            "svůj","svoje","svého","svému","svým","svém","svou","svoji","svých","svými",

            // Demonstrative pronouns and adjectives
            "ten","ta","to","ti","ty","ta","toho","tomu","tím","tom","tu","tou",
            "těch","těm","těmi","tento","tato","toto","tito","tyto","tato",
            "tamten","tamta","tamto","tamti","tamty","tamta","takový","taková","takové",

            // Interrogative and relative words
            "kdo","co","který","která","které","kteří","kterých","kterým","kterými",
            "čí","jaký","jaká","jaké","jac","jak","kde","kdy","proč","jak","odkud","kam",
            "kolik","dokud","pokud","jestli","zda","zdali",

            // Conjunctions
            "a","i","ale","nebo","či","že","když","protože","jelikož","poněvadž",
            "aby","kdyby","jestliže","pak","tak","tedy","proto","avšak","však",
            "nicméně","přesto","zatímco","dokud","než","jakmile","sotva",

            // Prepositions
            "v","ve","na","z","ze","od","pro","při","po","před","za","nad","pod",
            "mezi","bez","přes","ke","ku","do","k","s","se","u","o","ob","při",
            "proti","podle","během","kolem","vedle","mimo","kromě","místo","vůči",
            "díky","kvůli","skrz","naproti","uvnitř","venku","nahoře","dole",

            // Negative and indefinite words
            "ne","není","nejsou","nebyl","nebyla","nebyli","nic","nikdo","nikdy",
            "nikde","žádný","žádná","žádné","něco","někdo","někdy","někde",
            "nějaký","nějaká","nějaké","každý","každá","každé","všichni","všechny",
            "všechno","vše","jiný","jiná","jiné","jiní","ostatní",

            // Auxiliary / modal / copula words
            "být","jsem","jsi","jsme","jste","jsou","byl","byla","bylo","byli",
            "byly","být","budu","budeš","bude","budeme","budete","budou",
            "mít","mám","máš","má","máme","máte","mají","měl","měla","měli",
            "moci","můžu","můžeš","může","můžeme","můžete","mohou","mohl","mohla",
            "muset","musím","musíš","musí","musíme","musíte","musí","musel","musela",
            "chtít","chci","chceš","chce","chceme","chcete","chtějí","chtěl","chtěla",

            // Common adverbs
            "velmi","moc","hodně","málo","více","méně","nejvíce","nejméně",
            "teď","nyní","dnes","včera","zítra","ráno","večer","noc","odpoledne",
            "vždy","nikdy","často","někdy","zřídka","obvykle","už","ještě",
            "zde","tam","tady","pak","potom","nyní","brzy","pozdě","dobře","špatně",

            // Discourse markers
            "tedy","proto","takže","totiž","například","zejména","především",
            "konečně","nakonec","nejprve","nejdříve","kromě toho","kromě toho",
            "naopak","jinými slovy","stručně","celkem"
        };

        // Czech special characters: háček (č, ď, ě, ň, ř, š, ť, ž) and čárka (á, é, í, ó, ú, ů, ý)
        private static readonly Regex CzechCharRegex = new Regex(
            "[áčďéěíňóřšťúůýžÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ]",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Common Czech function words (used by the keyword regex)
        private static readonly Regex CzechKeywordRegex = new Regex(
            @"\b(já|ty|on|ona|ono|vy|oni|ony|jsem|jsi|jsou|byl|byla|bylo|byli|být|mám|máš|má|máme|máte|mají|mít|co|kdo|jak|kde|kdy|proč|toto|tento|tato|jeden|jedna|jedno|ale|nebo|že|když|protože|pokud|jako|také|jen|už|ještě|více|méně|než|tak|zde|tam|tady|pak|dnes|teď|již|velmi|se|si|ho|mu|jej|ji|nás|vás|nich|ve|na|z|ze|od|pro|při|po|před|za|nad|pod|mezi|bez|přes|ke|ku)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Letters (Latin script + others), used as denominator
        private static readonly Regex ScriptLetterRegex = new Regex(
            @"[\p{L}]",
            RegexOptions.Compiled);

        // ---- Czech / Slovak disambiguation ----

        // Czech-specific characters not used in Slovak: ř, ě, ů
        private static readonly Regex CzechSpecificCharRegex = new Regex(
            @"[řěůŘĚŮ]",
            RegexOptions.Compiled);

        // Slovak-specific characters not used in Czech: ľ, ĺ, ŕ, ô, ä
        private static readonly Regex SlovakSpecificCharRegex = new Regex(
            @"[ľĺŕôäĽĹŔÔÄ]",
            RegexOptions.Compiled);

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return CzechKeywordRegex.Matches(Input).Count;
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
            const double MinCharRatio = 0.02;
            const double MaxFailScore = 0.49;
            const double KeywordBonus = 0.1;
            const double StrongSignalScore = 0.7;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int LetterCount = ScriptLetterRegex.Matches(Input).Count;

            if (LetterCount == 0)
                return 0;

            int SpecialCharCount = CzechCharRegex.Matches(Input).Count;
            double SpecialCharRatio = (double)SpecialCharCount / LetterCount;

            // Base score driven by Czech special character ratio
            double Score = SpecialCharRatio >= MinCharRatio
                ? PassScore + (1 - PassScore) * (SpecialCharRatio - MinCharRatio) / (1 - MinCharRatio)
                : MaxFailScore * SpecialCharRatio / MinCharRatio;

            bool HasCzechSpecific = CzechSpecificCharRegex.IsMatch(Input);
            bool HasSlovakSpecific = SlovakSpecificCharRegex.IsMatch(Input);

            // Strong Czech signal: ř/ě/ů are not used in Slovak
            if (HasCzechSpecific)
            {
                Score = Math.Max(Score, StrongSignalScore);
            }
            // Slovak-specific characters without Czech-specific: likely not Czech
            else if (HasSlovakSpecific)
            {
                Score = Math.Min(Score, MaxFailScore);
            }

            int KeywordHits = CountKeywordHits(Input);

            // One Czech keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            // Slovak-specific characters still cap final score below PassScore
            if (!HasCzechSpecific && HasSlovakSpecific)
                Score = Math.Min(Score, MaxFailScore);

            return Math.Min(1.0, Score);
        }
    }
}