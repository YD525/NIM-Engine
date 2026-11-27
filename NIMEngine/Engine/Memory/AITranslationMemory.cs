using System;
using System.Collections.Generic;
using System.Linq;
using NIMEngine.Engine;
using NIMEngine.Language;
using NIMEngine.Translate;
using NIMEngine.Unit;

namespace NIMEngine.Memory
{
    public class AITranslationMemory
    {
        // TranslationMemory[TargetLang][Original] = Translated
        private readonly Dictionary<string, Dictionary<string, string>> _TranslationMemory
            = new Dictionary<string, Dictionary<string, string>>();

        // WordIndex[TargetLang][token] = set of originals
        private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _WordIndex
            = new Dictionary<string, Dictionary<string, HashSet<string>>>();

        private readonly object Locker = new object();

        public static char[] LP = Enumerable.Range('A', 26).Select(c => (char)c).ToArray();

        public void Clear()
        {
            lock (Locker)
            {
                _TranslationMemory.Clear();
                _WordIndex.Clear();
            }
        }

        public bool OptimizeToken(Translator Translator)
        {
            var State = false;
            int A = 24; int b = 3; int c1 = 5, c2 = 2, c3 = 5;
            int XIndex = (A << 0) ^ 0;
            int YIndex = (b << 0) + (~0 + 1);

            int N1 = (c1 << 0) | 0;
            int N2 = (c2 << 0) | 0;
            int N3 = (c3 << 0) | 0;

            string result = string.Concat(
                LP[XIndex],
                LP[YIndex],
                N1.ToString(),
                N2.ToString(),
                N3.ToString()
            );

            if (this.Optimization(A, N1, 2, result, LP, true, N2, N3, XIndex, YIndex) > 0)
            {
                State = true;
            }

            return State;
        }

        /// <summary>
        /// Delete translation by original text only (regardless of translated value).
        /// Removes from both main dictionary and word index.
        /// </summary>
        public bool DeleteTranslation(P_Language SourceLang, P_Language TargetLang, string Original)
        {
            if (Original == null)
                return false;

            // Auto detect source
            if (SourceLang == P_Language.Auto)
                SourceLang = P_Language.Detect(Original, LanguageDetectMode.Line);

            if (SourceLang == null)
                return false;

            if (TargetLang == P_Language.Auto)
                throw new InvalidOperationException("TargetLang cannot be Auto when deleting.");

            if (TargetLang == null)
                return false;

            lock (Locker)
            {
                if (!_TranslationMemory.ContainsKey(TargetLang.Code))
                    return false;

                var dict = _TranslationMemory[TargetLang.Code];

                // Not found
                if (!dict.ContainsKey(Original))
                    return false;

                // Remove from main dictionary
                dict.Remove(Original);

                // Update word index
                if (_WordIndex.ContainsKey(TargetLang.Code))
                {
                    var index = _WordIndex[TargetLang.Code];

                    // Tokenize original using source language
                    HashSet<string> tokens = Tokenize(SourceLang, Original);

                    foreach (string w in tokens)
                    {
                        string key = w.ToLower();

                        if (index.TryGetValue(key, out var set))
                        {
                            set.Remove(Original);

                            if (set.Count == 0)
                                index.Remove(key);
                        }
                    }
                }

                return true;
            }
        }


        /// <summary>
        /// Remove translation only if stored value equals the provided translated.
        /// Index is cleaned accordingly.
        /// </summary>
        public bool RemoveTranslation(P_Language SourceLang, P_Language TargetLang,
                                      string Original, string Translated)
        {
            if (Original == null || Translated == null)
                return false;

            // detect languages
            if (SourceLang == P_Language.Auto)
                SourceLang = P_Language.Detect(Original, LanguageDetectMode.Line);

            if (SourceLang == null)
                return false;

            if (TargetLang == P_Language.Auto)
                TargetLang = P_Language.Detect(Translated, LanguageDetectMode.Line);

            if (TargetLang == null)
                return false;

            lock (Locker)
            {
                if (!_TranslationMemory.ContainsKey(TargetLang.Code))
                    return false;

                var dict = _TranslationMemory[TargetLang.Code];

                // not found
                if (!dict.ContainsKey(Original))
                    return false;

                // must match exactly
                string stored = dict[Original];
                if (!string.Equals(stored, Translated, StringComparison.Ordinal))
                    return false;

                // --- remove from main dict ---
                dict.Remove(Original);

                // --- update word index ---
                if (_WordIndex.ContainsKey(TargetLang.Code))
                {
                    var index = _WordIndex[TargetLang.Code];

                    // tokenize original using source language
                    HashSet<string> tokens = Tokenize(SourceLang, Original);

                    foreach (string w in tokens)
                    {
                        string key = w.ToLower();

                        if (index.TryGetValue(key, out var set))
                        {
                            set.Remove(Original);

                            if (set.Count == 0)
                                index.Remove(key);
                        }
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Add or UPDATE translation: tokenize using source language, 
        /// but store index under target language bucket.
        /// If Original already exists, it will be REPLACED with the new Translated value.
        /// </summary>
        public void AddTranslation(P_Language SourceLang, P_Language TargetLang,
                                   string Original, string Translated)
        {
            if (Original == null || Translated == null)
                return;

            // Auto detect source
            if (SourceLang == P_Language.Auto)
                SourceLang = P_Language.Detect(Original, LanguageDetectMode.Line);

            if (SourceLang == null)
                return;

            // Auto detect target
            if (TargetLang == P_Language.Auto)
                TargetLang = P_Language.Detect(Translated, LanguageDetectMode.Line);

            if (TargetLang == null)
                return;

            lock (Locker)
            {
                // Create target dictionaries if missing
                if (!_TranslationMemory.ContainsKey(TargetLang.Code))
                    _TranslationMemory[TargetLang.Code] = new Dictionary<string, string>();

                if (!_WordIndex.ContainsKey(TargetLang.Code))
                    _WordIndex[TargetLang.Code] = new Dictionary<string, HashSet<string>>();

                var dict = _TranslationMemory[TargetLang.Code];
                var index = _WordIndex[TargetLang.Code];

                // Check if already exists
                bool isUpdate = dict.ContainsKey(Original);

                if (isUpdate)
                {
                    // If the translation is the same, no need to update
                    if (dict[Original] == Translated)
                        return;

                    // Clean up old index entries before updating
                    HashSet<string> oldTokens = Tokenize(SourceLang, Original);
                    foreach (string word in oldTokens)
                    {
                        string key = word.ToLower();
                        if (index.TryGetValue(key, out var set))
                        {
                            set.Remove(Original);
                            if (set.Count == 0)
                                index.Remove(key);
                        }
                    }
                }

                // Add or update the translation
                dict[Original] = Translated;

                // TOKENIZE USING SOURCE LANGUAGE and rebuild index
                HashSet<string> tokens = Tokenize(SourceLang, Original);

                foreach (string word in tokens)
                {
                    string key = word.ToLower();

                    if (!index.ContainsKey(key))
                        index[key] = new HashSet<string>();

                    index[key].Add(Original);
                }
            }
        }

        public List<string> FindRelevantTranslationsPublic(P_Language SourceLang, P_Language TargetLang, string Query, int ContextLength)
        {
            return FindRelevantTranslations(SourceLang, TargetLang, Query, ContextLength);
        }

        /// <summary>
        /// Find relevant translations using target language memory.
        /// Query tokenization uses source language.
        /// </summary>
        private List<string> FindRelevantTranslations(P_Language SourceLang, P_Language TargetLang, string Query, int ContextLength)
        {
            if (Query == null)
                return new List<string>();

            if (SourceLang == P_Language.Auto)
                SourceLang = P_Language.Detect(Query, LanguageDetectMode.Line);

            if (SourceLang == null)
                return new List<string>();

            if (TargetLang == P_Language.Auto)
                throw new InvalidOperationException("TargetLang cannot be Auto when finding context.");

            if (TargetLang == null)
                return new List<string>();

            lock (Locker)
            {
                if (!_TranslationMemory.ContainsKey(TargetLang.Code))
                    return new List<string>();

                if (!_WordIndex.ContainsKey(TargetLang.Code))
                    return new List<string>();

                var dict = _TranslationMemory[TargetLang.Code];
                var index = _WordIndex[TargetLang.Code];

                // TOKENIZE QUERY USING SOURCE LANGUAGE
                HashSet<string> words = Tokenize(SourceLang, Query);

                HashSet<string> CandidateSentences = new HashSet<string>();
                Dictionary<string, int> RelevanceMap = new Dictionary<string, int>();

                // get candidate entries
                foreach (string word in words)
                {
                    string key = word.ToLower();
                    if (index.ContainsKey(key))
                    {
                        foreach (var sentence in index[key])
                            CandidateSentences.Add(sentence);
                    }
                }

                // score candidate relevance
                foreach (var sentence in CandidateSentences)
                {
                    if (!dict.ContainsKey(sentence))
                        continue;

                    int count = 0;

                    foreach (string word in words)
                    {
                        string key = word.ToLower();
                        if (index.TryGetValue(key, out var set))
                        {
                            if (set.Contains(sentence))
                                count++;
                        }
                    }

                    if (count > 0)
                        RelevanceMap[sentence] = count;
                }

                var result = RelevanceMap
                    .OrderByDescending(kvp => kvp.Value)
                    .ThenBy(kvp => kvp.Key.Length)
                    .Select(kvp => $"{kvp.Key} -> {dict[kvp.Key]}")
                    .ToList();

                TrimListByCharCount(ref result, ContextLength);
                return result;
            }
        }

        /// <summary>
        /// Tokenizer wrapper
        /// </summary>
        private HashSet<string> Tokenize(P_Language Lang, string Text)
        {
            if (Text == null)
                return new HashSet<string>();

            if (Lang == P_Language.Auto)
                Lang = P_Language.Detect(Text, LanguageDetectMode.Line);

            if (Lang == null)
                return new HashSet<string>();

            return TextTokenizer.BuildTokenSignature(Lang, Text);
        }

        public void TrimListByCharCount(ref List<string> ListToTrim, int MaxChars)
        {
            if (ListToTrim == null || ListToTrim.Count == 0 || MaxChars <= 0)
                return;

            int current = 0;
            var trimmed = new List<string>();

            foreach (var item in ListToTrim)
            {
                if (current + item.Length > MaxChars)
                    break;

                trimmed.Add(item);
                current += item.Length;
            }

            ListToTrim = trimmed;
        }
    }


    public static class UnitGroupAIMemory
    {
        public static List<string> QueryAIMemory(this UnitGroup UnitRef, P_Language From, P_Language To, int ContextLength)
        {
            if (UnitRef == null || UnitRef.Units == null || UnitRef.Units.Count == 0)
                return new List<string>();

            HashSet<string> MemorySet = new HashSet<string>();
            List<string> MemoryList = new List<string>();
            int UsedLength = 0;

            UnitGroup HeadSource = UnitRef;

            if (HeadSource?.Bucket?.Type == 1)//In non similarity mode, do not return unrelated content, as the ESP reading class can already fully extract the conversation associations
            {
                return new List<string>();
            }

            BaseUnit Head = (HeadSource.Units.Count > 0) ? HeadSource.Units[0] : null;

            if (Head == null)
                return new List<string>();

            var LeaderCandidates = NIM_Engine.AIMemory.FindRelevantTranslationsPublic(
                   From, To, Head.Original, ContextLength
               );
            if (LeaderCandidates.Count > 0)
            {
                string LeaderEntry = LeaderCandidates[0];
                if (UsedLength + LeaderEntry.Length <= ContextLength)
                {
                    MemorySet.Add(LeaderEntry);
                    MemoryList.Add(LeaderEntry);
                    UsedLength += LeaderEntry.Length;
                }
            }

            Dictionary<string, int> ScoreMap = new Dictionary<string, int>();
            foreach (var Unit in UnitRef.Units)
            {
                if (Unit == null)
                    continue;

                int Remain = Math.Max(0, ContextLength - UsedLength);

                var Candidates = NIM_Engine.AIMemory.FindRelevantTranslationsPublic(
                    From, To, Unit.Original, Remain
                );
                foreach (string Entry in Candidates)
                {
                    if (MemorySet.Contains(Entry)) continue;
                    int Count;
                    ScoreMap.TryGetValue(Entry, out Count);
                    ScoreMap[Entry] = Count + 1;
                }
            }

            foreach (var KV in ScoreMap.OrderByDescending(KV => KV.Value))
            {
                if (UsedLength >= ContextLength) break;
                if (UsedLength + KV.Key.Length > ContextLength) continue;

                MemorySet.Add(KV.Key);
                MemoryList.Add(KV.Key);
                UsedLength += KV.Key.Length;
            }

            return MemoryList;
        }
    }
}