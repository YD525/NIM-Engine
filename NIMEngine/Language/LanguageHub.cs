using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{
    public enum LanguageDetectMode
    {
        Line,       // Detect each line independently
        WholeText   // Detect the entire text as a whole
    }
    public class P_Language_Ext
    {
        public static readonly P_Language English = new P_Language("en", "English");
        public static readonly P_Language German = new P_Language("de", "German");
    }

    public class P_Language : P_Language_Ext
    {
        public string Code = "";
        public string Name = "";
        public P_Language(string Code, string Name)
        {
            this.Code = Code;
            this.Name = Name;
        }
        public P_Language()
        {
            this.Code = string.Empty;
            this.Name = string.Empty;
        }

        public static P_Language NULL = new P_Language();
        public const string Auto = "(Auto)";
        public static readonly P_Language P_Auto = new P_Language("", "(Auto)");
        public static P_Language Detect(string Text, LanguageDetectMode DetectMode)
        {
            string Str = String.Copy(Text);

            Str = Regex.Replace(Str, @"([.!?。！？．｡؟۔።፧։])", "$1\n");

            Dictionary<string, double> Scores = new Dictionary<string, double>();

            if (DetectMode == LanguageDetectMode.Line)
            {
                foreach (var Key in new HashSet<string>(LanguageHub.SupportedNames))
                {
                    if (Scores.ContainsKey(Key))
                    {
                        Scores[Key] += LanguageHub.Languages[Key].GetProbability(Str);
                    }
                    else
                    {
                        Scores[Key] = LanguageHub.Languages[Key].GetProbability(Str);
                    }
                }
            }
            else
            {
                foreach (var GetLine in Str.Split(new char[2] { '\r', '\n' }))
                {
                    if (GetLine.Trim().Length > 0)
                    {
                        foreach (var Key in new HashSet<string>(LanguageHub.SupportedNames))
                        {
                            if (Scores.ContainsKey(Key))
                            {
                                Scores[Key] += LanguageHub.Languages[Key].GetProbability(GetLine);
                            }
                            else
                            {
                                Scores[Key] = LanguageHub.Languages[Key].GetProbability(GetLine);
                            }
                        }
                    }
                }
            }

            if (Scores.Count == 0) return null;

            var Best = Scores.OrderByDescending(Kv => Kv.Value).First();

            var SetLang = LanguageHub.Languages[Best.Key];

            if (Best.Value >= SetLang.PassScore)
            {
                return new P_Language(SetLang.Code, SetLang.LanguageName);
            }

            return null;   
        }

        public bool IsSpaceDelimitedLanguage()
        {
            switch (this.Name)
            {
                case "German":
                case "English":
                case "Turkish":
                case "Brazilian":
                case "Ukrainian":
                case "Russian":
                case "Italian":
                case "Spanish":
                case "Indonesian":
                case "Hindi":
                case "Urdu":
                case "French":
                case "Vietnamese":
                case "Polish":
                case "Persian":
                case "Czech":
                    return true;
                default:
                    return false;
            }
        }
        public bool IsNoSpaceLanguage()
        {
            switch (this.Name)
            {
                case "Japanese":
                case "Korean":
                case "TraditionalChinese":
                case "Thai":
                case "SimplifiedChinese":
                    return true;
                default:
                    return false;
            }
        }

        public static bool operator ==(P_Language Left, string Right)
        {
            if (Left is null && Right is null) return true;
            if (Left is null || Right is null) return false;
            return Left.Name == Right;
        }
        public static bool operator !=(P_Language Left, string Right)
        {
            return !(Left == Right);
        }
        public static bool operator ==(string Left, P_Language Right)
        {
            return Right == Left;
        }
        public static bool operator !=(string Left, P_Language Right)
        {
            return !(Right == Left);
        }
        public override bool Equals(object Obj)
        {
            if (Obj is P_Language Other)
            {
                return this.Code == Other.Code && this.Name == Other.Name;
            }
            if (Obj is string Str)
            {
                return this.Name == Str;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return ((Code ?? string.Empty) + "|" + (Name ?? string.Empty)).GetHashCode();
        }
    }

    public class LanguageHub
    {
        public static ConcurrentDictionary<string, string> LanguagesByCode = new ConcurrentDictionary<string, string>();
        public static HashSet<string> SupportedNames = new HashSet<string>();
        public static ConcurrentDictionary<string,I_Language> Languages = new ConcurrentDictionary<string, I_Language>();

        private static void AddLanguage(I_Language Lang)
        {
            LanguagesByCode[Lang.Code] = Lang.LanguageName;
            SupportedNames.Add(Lang.LanguageName);
            Languages[Lang.LanguageName] = Lang;
        }
        public static P_Language GetLanguageByCode(string Code)
        {
            if (string.IsNullOrEmpty(Code))
                return null;

            string LanguageName;
            if (LanguagesByCode.TryGetValue(Code, out LanguageName))
            {
                I_Language Lang;
                if (Languages.TryGetValue(LanguageName, out Lang))
                {
                    return new P_Language(Lang.Code,Lang.LanguageName);
                }
            }

            return null;
        }

        #region Old

        // Legacy compatibility only. Maps a language name back to the old Languages enum int value.
        // Do not use this for new code. 
        public static string GetLanguageCodeByLegacyCode(int code)
        {
            switch (code)
            {
                case 1: return "en";  // English
                case 2: return "de";  // German
                case 3: return "fr";  // French
                case 6: return "es";  // Spanish
                case 7: return "it";  // Italian
                case 8: return "pt";  // Portuguese
                case 9: return "pt-BR"; // Brazilian
                case 10: return "pl"; // Polish
                case 11: return "tr"; // Turkish
                case 12: return "vi"; // Vietnamese
                case 13: return "id"; // Indonesian
                case 20: return "ru"; // Russian
                case 21: return "uk"; // Ukrainian
                case 22: return "cs"; // Czech
                case 30: return "hi"; // Hindi
                case 31: return "ur"; // Urdu
                case 32: return "fa"; // Persian
                case 33: return "ar"; // Arabic
                case 50: return "zh-TW"; // TraditionalChinese
                case 51: return "zh-CN"; // SimplifiedChinese
                case 52: return "ja"; // Japanese
                case 53: return "ko"; // Korean
                case 55: return "th"; // Thai
                default: return null;
            }
        }
        #endregion

        public static void Init((bool Executed, Dictionary<string, object[]> Results) PluginData)
        {
            LanguagesByCode.Clear();
            SupportedNames.Clear();
            Languages.Clear();

            // Latin-based / common Western languages
            AddLanguage(new EnglishHelper());
            AddLanguage(new GermanHelper());
            AddLanguage(new FrenchHelper());
            AddLanguage(new SpanishHelper());
            AddLanguage(new ItalianHelper());
            AddLanguage(new PortugueseHelper());
            AddLanguage(new BrazilianPortugueseHelper());
            AddLanguage(new PolishHelper());
            AddLanguage(new TurkishHelper());
            AddLanguage(new VietnameseHelper());
            AddLanguage(new IndonesianHelper());

            // Slavic / Eastern European
            AddLanguage(new RussianHelper());
            AddLanguage(new UkrainianHelper());
            AddLanguage(new CzechHelper());

            // South Asian / Middle Eastern (RTL / special word boundaries)
            AddLanguage(new HindiHelper());
            AddLanguage(new UrduHelper());
            AddLanguage(new PersianHelper());
            AddLanguage(new ArabicHelper());

            // East Asian (no explicit word delimiters)
            AddLanguage(new TraditionalChineseHelper());
            AddLanguage(new SimplifiedChineseHelper());
            AddLanguage(new JapaneseHelper());
            AddLanguage(new KoreanHelper());
            AddLanguage(new ThaiHelper());

            if (PluginData.Executed)
            {
                foreach (var GetPluginResult in PluginData.Results)
                {
                    object[] Payload = GetPluginResult.Value;
                    if (Payload == null || Payload.Length < 1) continue;

                    if (!(Payload[0] is List<Type> List)) continue;

                    foreach (var Kv in List)
                    {
                        try
                        {
                            if (!typeof(I_Language).IsAssignableFrom(Kv)) continue;

                            I_Language Lang = (I_Language)Activator.CreateInstance(Kv);
                            AddLanguage(Lang);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }

        public static P_Language GetLang(string LanguageName)
        {
            if (Languages.ContainsKey(LanguageName))
            {
                var Lang = Languages[LanguageName];
                return new P_Language(Lang.Code,Lang.LanguageName);
            }
            return null;
        }
    }
}
