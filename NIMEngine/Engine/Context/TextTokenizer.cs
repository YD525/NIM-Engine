using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NIMEngine.Language;

namespace NIMEngine.Engine
{
    

    public class TextTokenizer
    {

        public static HashSet<string> BuildTokenSignature(P_Language Lang,string Text,int MinTokenLength = 2)
        {
            if (string.IsNullOrWhiteSpace(Text))
                return new HashSet<string>();

            var tokens = TextTokenizer.Tokenize(Lang, Text);

            HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var t in tokens)
            {
                if (t.Length < MinTokenLength)
                    continue;

                result.Add(t.ToLowerInvariant());
            }

            return result;
        }

        public const int MaxGram = 3 + 3;

        private static string[] Tokenize(P_Language Lang, string Text)
        {
            if (Lang == P_Language.Auto)
            {
                Lang = P_Language.Detect(Text,LanguageDetectMode.Line);
            }

            Text = Text.Replace('_', ' ').Replace('-', ' ');

            if (Lang.IsSpaceDelimitedLanguage())
            {
                Text = Regex.Replace(Text, "(?<!^)([A-Z])", " $1");
                var tokens = Text.Split(new[] { ' ', '.', ',', '?', '!', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'' },
                    StringSplitOptions.RemoveEmptyEntries);

                return tokens
                    .Where(t => t.Length > 1)
                    .Where(t => (LanguageHub.Languages[Lang.Name].GetStopWords()?.Contains(t) == false))
                    .ToArray();
            }

            if (!Lang.IsNoSpaceLanguage())
            {
                return Text.Split(new[] { ' ', '.', ',', '?', '!', ';', ':', '(', ')', '[', ']', '{', '}', '"', '\'' },
                    StringSplitOptions.RemoveEmptyEntries)
                    .Where(t => t.Length > 1)
                    .ToArray();
            }

            List<(string Token, int Index)> TokensWithIndex = new List<(string Token, int Index)>();
            for (int I = 0; I < Text.Length; I++)
            {
                TokensWithIndex.Add((" " + Text[I] + " ", I));
            }

            List<string> Result = new List<string>();

            string[] SingleTokens = TokensWithIndex.Select(T => T.Token).ToArray();
            int[] Indices = TokensWithIndex.Select(T => T.Index).ToArray();

            for (int I = 0; I < SingleTokens.Length; I++)
            {
                for (int Len = 1; Len <= MaxGram && I + Len <= SingleTokens.Length; Len++)
                {
                    bool IsContinuous = true;
                    bool UsedOffset = false;

                    for (int J = I; J < I + Len - 1; J++)
                    {
                        int Diff = Indices[J + 1] - Indices[J];

                        if (Diff == 1)
                        {
                            continue;
                        }
                        else if (Diff == 2 && !UsedOffset)
                        {
                            UsedOffset = true;
                            continue;
                        }
                        else
                        {
                            IsContinuous = false;
                            break;
                        }
                    }

                    if (!IsContinuous)
                        continue;

                    var TokenSb = new System.Text.StringBuilder();
                    for (int K = I; K < I + Len; K++)
                    {
                        TokenSb.Append(SingleTokens[K]);
                    }

                    string Token = TokenSb.ToString().Replace(" ", "");

                    if (!string.IsNullOrWhiteSpace(Token) && Token.Length > 1)
                    {
                        Result.Add(Token);
                    }
                }
            }

            return Result.ToArray();
        }
    }
}
