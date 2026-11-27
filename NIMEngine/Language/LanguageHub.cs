using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NIM_Plugin.Language;

namespace NIMEngine.Language
{

    //public static class P_Language
    //{
    //    private static readonly Dictionary<Languages, string> LanguageCodeMap = new Dictionary<Languages, string>()
    //    {
    //        //[Languages.English] = "en",
    //        //[Languages.SimplifiedChinese] = "zh-CN",
    //        //[Languages.TraditionalChinese] = "zh-TW",
    //        //[Languages.Japanese] = "ja",
    //        //[Languages.German] = "de",
    //        //[Languages.Korean] = "ko",
    //        //[Languages.Turkish] = "tr",
    //        //[Languages.Brazilian] = "pt-BR",
    //        //[Languages.Portuguese] = "pt",
    //        //[Languages.Russian] = "ru",
    //        //[Languages.Ukrainian] = "uk",
    //        //[Languages.Czech] = "cs",
    //        //[Languages.Italian] = "it",
    //        //[Languages.Spanish] = "es",
    //        //[Languages.Hindi] = "hi",
    //        //[Languages.Urdu] = "ur",
    //        //[Languages.Indonesian] = "id",
    //        //[Languages.French] = "fr",
    //        //[Languages.Vietnamese] = "vi",
    //        //[Languages.Polish] = "pl",
    //        //[Languages.Thai] = "th",
    //        //[Languages.Persian] = "fa",
    //        //[Languages.Arabic] = "ar",
    //        //[Languages.Auto] = "auto",
    //        //[Languages.Null] = ""
    //    };

    //    private static readonly Dictionary<string, Languages> CodeToLanguageMap = new Dictionary<string, Languages>(StringComparer.OrdinalIgnoreCase);
    //    static P_Language()
    //    {
    //        foreach (var pair in LanguageCodeMap)
    //        {
    //            if (!string.IsNullOrWhiteSpace(pair.Value))
    //            {
    //                CodeToLanguageMap[pair.Value] = pair.Key;
    //            }
    //        }
    //    }
    //    public static string ToLanguageCode(Languages Lang)
    //    {
    //        return LanguageCodeMap.TryGetValue(Lang, out var Code) ? Code : "";
    //    }
    //    public static Languages FromLanguageCode(string Code)
    //    {
    //        if (string.IsNullOrWhiteSpace(Code))
    //            return Languages.Null;

    //        return CodeToLanguageMap.TryGetValue(Code, out var Lang) ? Lang : Languages.Null;
    //    }
    //    public static void DetectLanguage(ref LanguageDetector OneDetect, string Str)
    //    {
    //        if (string.IsNullOrWhiteSpace(Str))
    //            return;

    //        double Score = 0;
    //        var Lang = DetectLanguageByContent(Str, ref Score);

    //        OneDetect.Add(Lang, Score);
    //    }

    //    public static Languages FromInt(int Value)
    //    {
    //        return Enum.IsDefined(typeof(Languages), Value)
    //            ? (Languages)Value
    //            : Languages.Auto;
    //    }

    //    public static Languages DetectLanguageByLine(string String)
    //    {
    //        LanguageDetector OneDetect = new LanguageDetector();

    //        DetectLanguage(ref OneDetect, String);
    //        return OneDetect.GetMaxLang();
    //    }
    //    private static Languages DetectLanguageByContent(string Text, ref double Score)
    //    {
    //        if (string.IsNullOrWhiteSpace(Text)) return Languages.English;

    //        var Scores = new Dictionary<Languages, double>();
    //        var Lines = Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    //        foreach (var Line in Lines)
    //        {
    //            if (string.IsNullOrWhiteSpace(Line)) continue;
    //            var LineScores = DetectLineScores(Line);
    //            foreach (var Kv in LineScores)
    //            {
    //                if (!Scores.ContainsKey(Kv.Key))
    //                    Scores[Kv.Key] = 0;
    //                Scores[Kv.Key] += Kv.Value;
    //            }
    //        }

    //        if (Lines.Length > 0)
    //        {
    //            foreach (var Key in Scores.Keys.ToList())
    //                Scores[Key] /= Lines.Length;
    //        }

    //        if (Scores.TryGetValue(Languages.English, out double EngScore) && EngScore > 0.5)
    //            return Languages.English;

    //        if (Scores.Count == 0) return Languages.English;

    //        var Best = Scores.OrderByDescending(Kv => Kv.Value).First();
    //        if (Best.Value < 0.01) return Languages.English;

    //        Score = Best.Value;

    //        return Best.Key;
    //    }
    //    private static Dictionary<Languages, double> DetectLineScores(string Line)
    //    {
    //        var Scores = new Dictionary<Languages, double>();
    //        void AddScore(Languages Lang, double Score)
    //        {
    //            if (Score > 0) Scores[Lang] = Score;
    //        }

    //        if (EnglishHelper.IsProbablyEnglish(Line))
    //            AddScore(Languages.English, 1.0);

    //        if (RussianHelper.ContainsRussian(Line))
    //            AddScore(Languages.Russian, RussianHelper.GetRussianRatio(Line));

    //        if (UkrainianHelper.IsProbablyUkrainian(Line))
    //            AddScore(Languages.Ukrainian, UkrainianHelper.GetUkrainianScore(Line));

    //        if (JapaneseHelper.IsProbablyJapanese(Line))
    //            AddScore(Languages.Japanese, JapaneseHelper.GetJapaneseScore(Line));
    //        else
    //        {
    //            var ZhType = ChineseVariantMap.CheckLangType(Line);
    //            if (ZhType == ZHType.Traditional)
    //                AddScore(Languages.TraditionalChinese, 0.02);
    //            else if (ZhType == ZHType.Simplified)
    //                AddScore(Languages.SimplifiedChinese, 0.02);
    //        }

    //        if (KoreanHelper.IsProbablyKorean(Line))
    //            AddScore(Languages.Korean, KoreanHelper.GetKoreanScore(Line));

    //        if (FrenchHelper.IsProbablyFrench(Line))
    //        {
    //            var Score = FrenchHelper.GetFrenchScore(Line);
    //            AddScore(Languages.French, Score);
    //            if (CanadianFrenchHelper.IsProbablyCanadianFrench(Line))
    //                AddScore(Languages.CanadianFrench, Score * 1.2);
    //        }

    //        if (PortugueseHelper.IsProbablyPortuguese(Line))
    //        {
    //            var Score = PortugueseHelper.GetPortugueseScore(Line);
    //            AddScore(Languages.Portuguese, Score);
    //            if (BrazilianPortugueseHelper.IsProbablyBrazilianPortuguese(Line))
    //                AddScore(Languages.Brazilian, Score * 1.1);
    //        }

    //        if (GermanHelper.IsProbablyGerman(Line))
    //            AddScore(Languages.German, GermanHelper.GetGermanScore(Line));

    //        if (ItalianHelper.IsProbablyItalian(Line))
    //            AddScore(Languages.Italian, ItalianHelper.GetItalianScore(Line));

    //        if (SpanishHelper.IsProbablySpanish(Line))
    //            AddScore(Languages.Spanish, SpanishHelper.GetSpanishScore(Line));

    //        if (PolishHelper.IsProbablyPolish(Line))
    //            AddScore(Languages.Polish, PolishHelper.GetPolishScore(Line));
    //        else if (CzechHelper.IsProbablyCzech(Line))
    //            AddScore(Languages.Czech, CzechHelper.GetCzechScore(Line));

    //        if (TurkishHelper.IsProbablyTurkish(Line))
    //            AddScore(Languages.Turkish, TurkishHelper.GetTurkishScore(Line));

    //        if (HindiHelper.IsProbablyHindi(Line))
    //            AddScore(Languages.Hindi, HindiHelper.GetHindiScore(Line));
    //        else if (UrduHelper.IsProbablyUrdu(Line))
    //            AddScore(Languages.Urdu, UrduHelper.GetUrduScore(Line));

    //        if (IndonesianHelper.IsProbablyIndonesian(Line))
    //            AddScore(Languages.Indonesian, IndonesianHelper.GetIndonesianScore(Line));

    //        if (VietnameseHelper.IsProbablyVietnamese(Line))
    //            AddScore(Languages.Vietnamese, VietnameseHelper.GetVietnameseScore(Line));

    //        if (ThaiHelper.IsProbablyThai(Line))
    //            AddScore(Languages.Thai, ThaiHelper.GetThaiScore(Line));

    //        if (ArabicHelper.IsProbablyArabic(Line))
    //            AddScore(Languages.Arabic, ArabicHelper.GetArabicScore(Line));
    //        else if (PersianHelper.IsProbablyPersian(Line))
    //            AddScore(Languages.Persian, PersianHelper.GetPersianScore(Line));

    //        return Scores;
    //    }
    //}
    public enum LanguageDetectMode
    {
        Line,       // Detect each line independently
        WholeText   // Detect the entire text as a whole
    }
    public class P_Language
    {
        public string Code = "";
        public string Name = "";
        public P_Language(string Code, string Name)
        {
            this.Code = Code;
            this.Name = Name;
        }

        public static P_Language Detect(string Str, LanguageDetectMode DetectMode)
        {
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
    }

    public class LanguageHub
    {
        public static HashSet<string> SupportedNames = new HashSet<string>();
        public static ConcurrentDictionary<string,I_Language> Languages = new ConcurrentDictionary<string, I_Language>();

        private static void AddLanguage(I_Language Lang)
        {
            SupportedNames.Add(Lang.LanguageName);
            Languages[Lang.LanguageName] = Lang;
        }

        //public enum Languages
        //{
        //    // Special
        //    Null = -1,
        //    Auto = 0,

        //    // Latin-based / common Western languages
        //    English = 1,
        //    German = 2,
        //    French = 3,
        //    Spanish = 6,
        //    Italian = 7,
        //    Portuguese = 8,
        //    Brazilian = 9,
        //    Polish = 10,
        //    Turkish = 11,
        //    Vietnamese = 12,
        //    Indonesian = 13,

        //    // Slavic / Eastern European
        //    Russian = 20,
        //    Ukrainian = 21,
        //    Czech = 22,

        //    // South Asian / Middle Eastern (RTL / special word boundaries)
        //    Hindi = 30,
        //    Urdu = 31,
        //    Persian = 32,
        //    Arabic = 33,

        //    // East Asian (no explicit word delimiters)
        //    TraditionalChinese = 50,
        //    SimplifiedChinese = 51,
        //    Japanese = 52,
        //    Korean = 53,
        //    Thai = 55
        //}

        public static void Init((bool Executed, Dictionary<string, object[]> Results) PluginData)
        {
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
