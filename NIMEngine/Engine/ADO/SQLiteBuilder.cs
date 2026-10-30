using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text.RegularExpressions;

namespace NIMEngine.ADO
{
    /// <summary>
    /// Creates SQLite parameters and constrains SQL fragments that cannot be represented by parameters.
    /// </summary>
    internal static class SQLiteBuilder
    {
        internal const string LanguageFilter = "WHERE [From] = @From AND [To] = @To";
        internal const string SourceLanguageFilter = "WHERE [Source] = @Source AND [From] = @From AND [To] = @To";

        private static readonly HashSet<string> AllowedIdentifiers = new HashSet<string>(
            StringComparer.Ordinal)
        {
            "AdvancedDictionary",
            "AdvancedDictionary_Old",
            "ChineseVariantMap",
            "CloudTranslation",
            "FontColors",
            "LocalTranslation",
            "RecordsHistory",
            "UniqueKeys",
            "Words"
        };

        private static readonly Regex ParameterName = new Regex(
            "^@[A-Za-z][A-Za-z0-9_]*$",
            RegexOptions.CultureInvariant);

        /// <summary>Creates a named parameter while preserving <see langword="null"/> as SQL NULL.</summary>
        /// <param name="Name">The fixed parameter name used by the command text.</param>
        /// <param name="Value">The data value to bind.</param>
        /// <returns>A caller-owned parameter ready to add to one command.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="Name"/> is not a valid parameter name.</exception>
        internal static SQLiteParameter Parameter(string Name, object Value)
        {
            if (string.IsNullOrEmpty(Name) || !ParameterName.IsMatch(Name))
                throw new ArgumentException("SQLite parameter names must use a fixed @name token.", nameof(Name));

            return new SQLiteParameter(Name, Value ?? DBNull.Value);
        }

        /// <summary>Quotes an identifier selected from the product schema allowlist.</summary>
        /// <param name="Identifier">The table identifier required by a fixed schema operation.</param>
        /// <returns>The allow-listed identifier enclosed in SQLite identifier quotes.</returns>
        /// <exception cref="ArgumentException">Thrown when the identifier is not part of the product schema.</exception>
        internal static string QuoteIdentifier(string Identifier)
        {
            if (string.IsNullOrEmpty(Identifier) || !AllowedIdentifiers.Contains(Identifier))
                throw new ArgumentException("The SQLite identifier is not part of the product schema.", nameof(Identifier));

            return "[" + Identifier + "]";
        }

        /// <summary>Returns a pagination filter only when it is one of the fixed supported templates.</summary>
        /// <param name="Filter">The SQL filter template without embedded data values.</param>
        /// <returns>The normalized, allow-listed filter.</returns>
        /// <exception cref="ArgumentException">Thrown when the filter is not a fixed supported template.</exception>
        internal static string RequirePaginationFilter(string Filter)
        {
            string Candidate = (Filter ?? string.Empty).Trim();
            if (Candidate.Length == 0 ||
                string.Equals(Candidate, LanguageFilter, StringComparison.Ordinal) ||
                string.Equals(Candidate, SourceLanguageFilter, StringComparison.Ordinal))
            {
                return Candidate;
            }

            throw new ArgumentException("The SQLite pagination filter is not allow-listed.", nameof(Filter));
        }
    }
}