using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIMEngine.ADO;
using NIMEngine.Language;

namespace NIMEngine.Engine
{
    public class ReplaceTag
    {
        public int Rowid = 0;
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public bool IsHint { get; set; } = false;
        public bool IsProtected { get; set; } = false;
        public ReplaceTag(string Key, string Value)
        {
            this.Key = Key;
            this.Value = Value;
        }
        public ReplaceTag(int Rowid, string Key, string Value)
        {
            this.Rowid = Rowid;
            this.Key = Key;
            this.Value = Value;
        }
    }

    public class TranslationPreprocessor : TranslationPreprocessorExtend
    {
        public bool HasPlaceholder = false;
        public string SourceStr = "";

        public List<ReplaceTag> ReplaceTags = new List<ReplaceTag>();

        public TranslationPreprocessor()
        {

        }

        public static TranslationPreprocessor Clone(TranslationPreprocessor Preprocessor)
        {
            TranslationPreprocessor NTranslationPreprocessor = new TranslationPreprocessor();
            NTranslationPreprocessor.HasPlaceholder = Preprocessor.HasPlaceholder;
            NTranslationPreprocessor.SourceStr = Preprocessor.SourceStr;
            NTranslationPreprocessor.ReplaceTags.AddRange(Preprocessor.ReplaceTags);
            return NTranslationPreprocessor;
        }

        public bool SecondaryQualityInspection(string Source, List<ReplaceTag> CustomWords)
        {
            if (string.IsNullOrEmpty(Source))
                return false;

            if (CustomWords == null || CustomWords.Count == 0)
                return true;

            HashSet<string> FoundIds = new HashSet<string>();

            string Pattern = @"[\[【\(（]\s*_\s*([Pp]?\d+)\s*[\]】\)）]";

            var Matches = Regex.Matches(Source, Pattern, RegexOptions.IgnoreCase);

            foreach (Match Match in Matches)
            {
                if (Match.Success)
                {
                    string FoundId = Match.Groups[1].Value.Trim().ToUpper();
                    FoundIds.Add(FoundId);
                }
            }

            HashSet<string> ExpectedIds = new HashSet<string>();

            foreach (var Word in CustomWords)
            {
                if (Word.IsHint)
                    continue;

                var IDMatch = Regex.Match(Word.Key, @"\[_([Pp]?\d+)\]");
                if (IDMatch.Success)
                {
                    string ID = IDMatch.Groups[1].Value.ToUpper();
                    ExpectedIds.Add(ID);
                }
            }

            return FoundIds.Count == ExpectedIds.Count && ExpectedIds.All(id => FoundIds.Contains(id));
        }

        /// <summary>
        /// ForceReplace = true  : Generates placeholders such as [_P0] (Key = placeholder, Value = original text).
        /// ForceReplace = false : Hint only (Key = Value = original text, IsHint = true); the source text is not modified.
        /// </summary>
        private List<ReplaceTag> GenerateProtectedTags(string Source, bool ForceReplace)
        {
            var Tags = new List<ReplaceTag>();
            int Index = 0;

            foreach (var Pattern in NIM_Engine.Config.ProtectedPatterns)
            {
                var Matches = Regex.Matches(Source, Pattern);
                foreach (Match Match in Matches)
                {
                    if (!Match.Success)
                        continue;

                    string Value = Match.Value;

                    if (Tags.Any(T => T.Value == Value))
                        continue;

                    if (ForceReplace)
                    {
                        string Placeholder = $"[_P{Index}]";
                        Tags.Add(new ReplaceTag(Placeholder, Value) { IsProtected = true });
                        Index++;
                    }
                    else
                    {
                        Tags.Add(new ReplaceTag(Value, Value)
                        {
                            IsHint = true,
                            IsProtected = true
                        });
                    }
                }
            }

            return Tags;
        }

        public string GeneratePlaceholderText(string FileName, Languages From, Languages To, string SourceStr, string Type, out bool NeedFurtherTranslate, bool ForceReplace, bool ForceReplaceProtected)
        {
            ReplaceTags.Clear();
            HasPlaceholder = false;
            bool UseWordBoundary = LanguageExtensions.IsSpaceDelimitedLanguage(From);

            var ProtectedTags = GenerateProtectedTags(SourceStr, ForceReplaceProtected);
            for (int i = 0; i < ProtectedTags.Count; i++)
            {
                if (ProtectedTags[i].Value == string.Empty)
                {
                    continue;
                }
                if (SourceStr.Contains(ProtectedTags[i].Value))
                {
                    var Source = ProtectedTags[i].Value;
                    var Placeholder = ProtectedTags[i].Key;

                    if (ProtectedTags[i].IsHint)
                    {
                        ReplaceTags.Add(ProtectedTags[i]);
                        continue;
                    }

                    if (UseWordBoundary)
                    {
                        string Pattern = Regex.Escape(Source);
                        if (Regex.IsMatch(SourceStr, Pattern, RegexOptions.IgnoreCase))
                        {
                            SourceStr = SourceStr.Replace(Source, Placeholder);
                            ReplaceTags.Add(ProtectedTags[i]);
                        }
                    }
                    else
                    {
                        SourceStr = SourceStr.Replace(Source, Placeholder);
                        ReplaceTags.Add(ProtectedTags[i]);
                    }
                }
            }

            string OriginalSourceStr = SourceStr;

            var Tags = AdvancedDictionary.Query(FileName, Type, From, To, SourceStr, UseWordBoundary);

            if (ForceReplace)
            {
                for (int i = 0; i < Tags.Count; i++)
                {
                    var Word = Tags[i];
                    string Placeholder = $"[_{i}]";
                    string Source = Word.Source;
                    if (UseWordBoundary)
                    {
                        string Pattern = Regex.Escape(Source);
                        if (Regex.IsMatch(SourceStr, Pattern, RegexOptions.IgnoreCase))
                        {
                            SourceStr = Regex.Replace(SourceStr, Pattern, Placeholder, RegexOptions.IgnoreCase);
                            ReplaceTags.Add(new ReplaceTag(Tags[i].Rowid, Placeholder, Word.Result));
                        }
                    }
                    else
                    {
                        if (SourceStr.Contains(Source))
                        {
                            SourceStr = SourceStr.Replace(Source, Placeholder);
                            ReplaceTags.Add(new ReplaceTag(Tags[i].Rowid, Placeholder, Word.Result));
                        }
                    }
                }
            }
            else
            {
                var FoundSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < Tags.Count; i++)
                {
                    var Word = Tags[i];
                    string Source = Word.Source;
                    bool Found = false;
                    if (UseWordBoundary)
                    {
                        string Pattern = Regex.Escape(Source);
                        if (Regex.IsMatch(SourceStr, Pattern, RegexOptions.IgnoreCase))
                        {
                            Found = true;
                        }
                    }
                    else
                    {
                        if (SourceStr.Contains(Source))
                        {
                            Found = true;
                        }
                    }

                    if (Found)
                    {
                        FoundSources.Add(Source);

                        ReplaceTags.Add(new ReplaceTag(Tags[i].Rowid, Word.Source, Word.Result)
                        {
                            IsHint = true
                        });
                    }
                }

                string CheckText = SourceStr;
                foreach (var Tag in ReplaceTags)
                {
                    if (!Tag.IsHint && Tag.Key.Contains("[_") && Tag.Key.Contains("]"))
                    {
                        CheckText = CheckText.Replace(Tag.Key, "");
                    }
                    else if (Tag.IsHint && Tag.IsProtected && !string.IsNullOrEmpty(Tag.Key))
                    {
                        CheckText = CheckText.Replace(Tag.Key, "");
                    }
                }

                foreach (var FoundSource in FoundSources)
                {
                    if (UseWordBoundary)
                    {
                        string Pattern = Regex.Escape(FoundSource);
                        CheckText = Regex.Replace(CheckText, Pattern, "", RegexOptions.IgnoreCase);
                    }
                    else
                    {
                        CheckText = CheckText.Replace(FoundSource, "");
                    }
                }

                CheckText = Regex.Replace(CheckText, @"[\s\u3000]", "");
                NeedFurtherTranslate = !string.IsNullOrWhiteSpace(CheckText.Trim());

                this.SourceStr = SourceStr;
                HasPlaceholder = ReplaceTags.Count > 0;
                return SourceStr;
            }


            string Residual = SourceStr;
            foreach (var Tag in ReplaceTags)
            {
                if (!Tag.IsHint && Tag.Key.Contains("[_") && Tag.Key.Contains("]"))
                {
                    Residual = Residual.Replace(Tag.Key, "");
                }
                else if (Tag.IsHint && Tag.IsProtected && !string.IsNullOrEmpty(Tag.Key))
                {
                    Residual = Residual.Replace(Tag.Key, "");
                }
            }
            Residual = Regex.Replace(Residual, @"[\s\u3000]", "");
            NeedFurtherTranslate = !string.IsNullOrWhiteSpace(Residual.Trim());
            HasPlaceholder = ReplaceTags.Count > 0;
            this.SourceStr = SourceStr;
            return SourceStr;
        }

        public string RestoreFromPlaceholder(string Str, Languages Lang, bool RestoreHints = false)
        {
            if (string.IsNullOrEmpty(Str) || ReplaceTags.Count == 0)
                return Str;

            string Result = Str;

            Dictionary<string, string> IDToValueMap = new Dictionary<string, string>();

            foreach (var Tag in ReplaceTags)
            {
                if (Tag.IsHint)
                    continue;

                var Match = Regex.Match(Tag.Key, @"\[_([Pp]?\d+)\]");
                if (Match.Success)
                {
                    string ID = Match.Groups[1].Value.ToUpper();
                    IDToValueMap[ID] = Tag.Value;
                }
            }

            string Pattern = @"[\[【\(（]\s*_\s*([Pp]?\d+)\s*[\]】\)）]";

            Result = Regex.Replace(Result, Pattern, Match =>
            {
                string ID = Match.Groups[1].Value.Trim().ToUpper();

                if (IDToValueMap.TryGetValue(ID, out string Value))
                {
                    return Value;
                }

                return Match.Value;
            }, RegexOptions.IgnoreCase);

            if (RestoreHints)
            {
                foreach (var Tag in ReplaceTags)
                {
                    if (Tag.IsHint && !Tag.IsProtected && !string.IsNullOrEmpty(Tag.Key) && !string.IsNullOrEmpty(Tag.Value))
                    {
                        Result = Result.Replace(Tag.Key, Tag.Value);
                    }
                }
            }

            return Result;
        }
    }
}