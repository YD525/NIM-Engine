using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public class RussianHelper : I_Language
    {
        public double PassScore { get; set; } = 0.5;

        private static readonly HashSet<string> StopWords =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // Conjunctions
            "и","а","но","да","или","либо","что","чтобы","если","когда","пока",
            "хотя","потому","поэтому","также","тоже","зато","однако","будто","словно",

            // Prepositions
            "в","во","на","с","со","к","ко","о","об","обо","у","за","из","изо",
            "от","ото","до","по","под","подо","над","при","про","для","без","через",
            "между","перед","передо","после","около","вокруг","среди","кроме",
            "вместо","ради","из-за","из-под",

            // Particles
            "не","ни","же","ли","бы","б","вот","вон","ведь","лишь","только",
            "даже","уж","уже","ещё","еще","разве","неужели","пусть","именно",

            // Personal and reflexive pronouns
            "я","меня","мне","мной","мною",
            "ты","тебя","тебе","тобой","тобою",
            "он","его","него","ему","нему","им","ним","нём","нем",
            "она","её","ее","неё","нее","ей","ней","ею","нею",
            "оно",
            "мы","нас","нам","нами",
            "вы","вас","вам","вами",
            "они","их","них","ими","ними",
            "себя","себе","собой","собою",

            // Possessive pronouns
            "мой","моя","моё","мое","мои","моего","моей","моему","моим","моих","моими","мою","моём","моем",
            "твой","твоя","твоё","твое","твои","твоего","твоей","твоему","твоим","твоих","твоими","твою","твоём","твоем",
            "наш","наша","наше","наши","нашего","нашей","нашему","нашим","наших","нашими","нашу","нашем",
            "ваш","ваша","ваше","ваши","вашего","вашей","вашему","вашим","ваших","вашими","вашу","вашем",
            "свой","своя","своё","свое","свои","своего","своей","своему","своим","своих","своими","свою","своём","своем",

            // Demonstrative and determinative pronouns
            "этот","эта","это","эти","этого","этой","этому","этим","этих","этими","эту","этом",
            "тот","та","то","те","того","той","тому","тем","тех","теми","ту","том",
            "такой","такая","такое","такие",
            "весь","вся","всё","все","всего","всей","всему","всем","всех","всеми","всю","всём",
            "каждый","каждая","каждое","каждые",
            "сам","сама","само","сами",
            "другой","другая","другое","другие",

            // Interrogative and relative pronouns
            "кто","кого","кому","кем","ком",
            "чего","чему","чем","чём",
            "какой","какая","какое","какие",
            "который","которая","которое","которые","которого","которой","которому","которым","которых","которыми","которую","котором",
            "чей","чья","чьё","чье","чьи",
            "где","куда","откуда","как","почему","зачем","сколько",

            // Negative and indefinite words
            "никто","ничто","ничего","никогда","нигде","никуда","нет",
            "кто-то","что-то","где-то","когда-то","как-то",

            // Auxiliary and modal words
            "быть","есть","был","была","было","были",
            "буду","будешь","будет","будем","будете","будут",
            "могу","можешь","может","можем","можете","могут","мог","могла","могли",
            "можно","нужно","надо","нельзя",

            // Common adverbs
            "очень","так","там","тут","здесь","туда","сюда",
            "тогда","теперь","сейчас","потом","затем","всегда","иногда","часто",
            "опять","снова","совсем","почти","более","менее","слишком",
            "сегодня","завтра","вчера"
        };

        // Words are runs of letters, hyphenated words are kept together
        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{M}]+(?:-[\p{L}\p{M}]+)*", RegexOptions.Compiled);

        // Cyrillic characters
        private static readonly Regex CyrillicCharRegex = new Regex(@"[\u0400-\u04FF]", RegexOptions.Compiled);

        public float Version { get; set; } = 1.1f;

        public int GetLanguage()
        {
            return (int)Languages.Russian;
        }

        public HashSet<string> GetStopWords()
        {
            return StopWords;
        }

        private static int CountKeywordHits(string Input)
        {
            return WordRegex.Matches(Input).Cast<Match>().Count(Word => StopWords.Contains(Word.Value));
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
            const double KeywordBonus = 0.1;

            if (string.IsNullOrWhiteSpace(Input))
                return 0;

            int CyrillicCount = CyrillicCharRegex.Matches(Input).Count;

            if (CyrillicCount == 0)
                return 0;

            // Spaces, digits and punctuation are not counted
            int LetterCount = Input.Count(char.IsLetter);

            double CyrillicRatio = (double)CyrillicCount / Math.Max(LetterCount, CyrillicCount);

            double Score = CyrillicRatio >= MinCyrillicRatio
                ? PassScore + (1 - PassScore) * (CyrillicRatio - MinCyrillicRatio) / (1 - MinCyrillicRatio)
                : MaxFailScore * CyrillicRatio / MinCyrillicRatio;

            int KeywordHits = CountKeywordHits(Input);

            // One keyword is enough to pass
            if (KeywordHits >= 1)
                Score = Math.Max(Score, PassScore) + KeywordBonus * KeywordHits;

            return Math.Min(1.0, Score);
        }
    }
}