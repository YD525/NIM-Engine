using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NIMEngine.Common;
using NIMEngine.Engine;
using NIMEngine.Language;

namespace NIMEngine.ADO
{
    public class LocalTranslationItem
    {
        public int FileUniqueKey = 0;
        public string Key = "";
        public string To = "";
        public string Source = "";
        public string Result = "";
        public int Index = 0;

        public LocalTranslationItem(int FileUniqueKey, string Key, P_Language TargetLanguage, string Source, string Result)
        {
            this.FileUniqueKey = FileUniqueKey;
            this.Key = Key;
            this.To = TargetLanguage.Code;
            this.Source = Source;
            this.Result = Result;
            this.Index = 0;
        }

        public LocalTranslationItem(object FileUniqueKey, object Key, object To, object Source, object Result)
        {
            this.FileUniqueKey = P_Convert.ObjToInt(FileUniqueKey);
            this.Key = P_Convert.ObjToStr(Key);
            this.To = P_Convert.ObjToStr(To);
            this.Source = P_Convert.ObjToStr(Source);
            this.Result = P_Convert.ObjToStr(Result);
            this.Index = 0;
        }
    }
    public class LocalDBCache
    {
        public static void Init()
        {
            string TableName = "LocalTranslation";
            string CreateSQL = "CREATE TABLE [LocalTranslation]([FileUniqueKey] INT, [Key] TEXT, [To] TEXT, [Source] TEXT, [Result] TEXT, [Index] INT);";

            // Check if table exists
            const string CheckTableSQL = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @TableName;";
            var Result = NIM_Engine.LocalDB.ExecuteScalar(
                CheckTableSQL,
                SQLiteBuilder.Parameter("@TableName", TableName));

            if (Result != null && Result != DBNull.Value)
            {
                // Table exists, check column structure
                var Columns = NIM_Engine.LocalDB.ExecuteQuery("PRAGMA table_info(LocalTranslation);");

                // Current columns
                var ExistingCols = new HashSet<string>(
                    Columns.AsEnumerable().Select(R => R["name"].ToString()),
                    StringComparer.OrdinalIgnoreCase
                );

                // Expected columns
                string[] ExpectedCols = { "FileUniqueKey", "Key", "To", "Source", "Result", "Index" };

                bool StructureChanged =
                    ExistingCols.Count != ExpectedCols.Length ||
                    ExpectedCols.Any(C => !ExistingCols.Contains(C));

                if (StructureChanged)
                {
                    NIM_Engine.LocalDB.ExecuteNonQuery(
                        "DROP TABLE IF EXISTS " + SQLiteBuilder.QuoteIdentifier(TableName) + ";");
                    NIM_Engine.LocalDB.ExecuteNonQuery(CreateSQL);
                }
                else
                {
                    //Check whether To column is TEXT (old INT structure needs migration)
                    var ToColumn = Columns.FirstOrDefault(R => R["name"].ToString() == "To");
                    bool IsToText = ToColumn != null && ToColumn["type"].ToString().ToUpper().Contains("TEXT");

                    if (!IsToText)
                    {
                        MigrateLanguageCodeColumn();
                    }
                }
            }
            else
            {
                // Create if not exists
                NIM_Engine.LocalDB.ExecuteNonQuery(CreateSQL);
            }
        }

        private static void MigrateLanguageCodeColumn()
        {
            NIM_Engine.LocalDB.ExecuteNonQuery("ALTER TABLE LocalTranslation RENAME TO LocalTranslation_Old;");

            NIM_Engine.LocalDB.ExecuteNonQuery("CREATE TABLE [LocalTranslation]([FileUniqueKey] INT, [Key] TEXT, [To] TEXT, [Source] TEXT, [Result] TEXT, [Index] INT);");

            //Read old rows first, then map legacy INT language ids to language codes
            List<Dictionary<string, object>> OldRows = NIM_Engine.LocalDB.ExecuteQuery("SELECT FileUniqueKey, Key, [To], Source, Result, [Index] FROM LocalTranslation_Old;");

            const string InsertSQL = "INSERT INTO LocalTranslation (FileUniqueKey, [Key], [To], [Source], [Result], [Index]) VALUES (@FileUniqueKey, @Key, @To, @Source, @Result, @Index);";

            for (int i = 0; i < OldRows.Count; i++)
            {
                var Row = OldRows[i];

                int LegacyTo = P_Convert.ObjToInt(Row["To"]);

                string ToCode = LanguageHub.GetLanguageCodeByLegacyCode(LegacyTo);

                NIM_Engine.LocalDB.ExecuteNonQuery(
                    InsertSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", Row["FileUniqueKey"]),
                    SQLiteBuilder.Parameter("@Key", Row["Key"]),
                    SQLiteBuilder.Parameter("@To", ToCode ?? string.Empty),
                    SQLiteBuilder.Parameter("@Source", Row["Source"]),
                    SQLiteBuilder.Parameter("@Result", Row["Result"]),
                    SQLiteBuilder.Parameter("@Index", Row["Index"]));
            }

            NIM_Engine.LocalDB.ExecuteNonQuery("DROP TABLE LocalTranslation_Old;");
        }

        public static List<LocalTranslationItem> MatchLocalItem(P_Language To, string Source, int Limit = 5)
        {
            new TranslationPreprocessor().OptimizeStrings(ref Source);

            try
            {
                List<LocalTranslationItem> LocalTransItems = new List<LocalTranslationItem>();

                const string SQLOrder = "SELECT * FROM LocalTranslation WHERE [To] = @To AND [Source] = @Source LIMIT @Limit;";
                List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@To", To.Code),
                    SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SQLiteBuilder.Parameter("@Limit", Limit));
                if (NTable.Count > 0)
                {
                    for (int i = 0; i < NTable.Count; i++)
                    {
                        var Row = NTable[i];

                        LocalTransItems.Add(new LocalTranslationItem(
                            Row["FileUniqueKey"],
                            Row["Key"],
                            P_Convert.ObjToStr(Row["To"]),
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"]))
                        ));
                    }
                }

                return LocalTransItems;
            }
            catch
            {
                return new List<LocalTranslationItem>();
            }
        }

        public static bool DeleteCacheByFileUniqueKey(int FileUniqueKey, P_Language TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@To", TargetLanguage.Code));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static bool DeleteCacheBySource(int FileUniqueKey, string Source, P_Language TargetLanguage)
        {
            new TranslationPreprocessor().OptimizeStrings(ref Source);

            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Source] = @Source AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SQLiteBuilder.Parameter("@To", TargetLanguage.Code));

                if (State != 0)
                {
                    return true;
                }

                return false;

            }
            catch { return false; }
        }

        public static bool DeleteCacheByResult(int FileUniqueKey, string ResultText, P_Language TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Result] = @Result AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(ResultText)),
                    SQLiteBuilder.Parameter("@To", TargetLanguage.Code));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static bool DeleteCache(int FileUniqueKey, string Key, P_Language TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", TargetLanguage.Code));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static string GetCacheText(int FileUniqueKey, string Key, P_Language TargetLanguage)
        {
            try
            {
                const string SQLOrder = "SELECT Result FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                string GetText = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", TargetLanguage.Code)));

                if (GetText.Trim().Length > 0)
                {
                    return SQLSafeCodec.Decode(GetText);
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static string FindCache(int FileUniqueKey, string Key, P_Language To)
        {
            try
            {
                const string SQLOrder = "SELECT Result FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                string GetResult = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", To.Code)));

                if (GetResult.Trim().Length > 0)
                {
                    return SQLSafeCodec.Decode(GetResult);
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool UpdateLocalTransItem(int FileUniqueKey, string Key, P_Language To, string Source, string Result, int Index)
        {
            if (Result.Length > 0)
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                int GetRowID = P_Convert.ObjToInt(NIM_Engine.LocalDB.ExecuteScalar("SELECT Rowid FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;",
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", To.Code)));

                if (GetRowID <= 0)
                {
                    var GetStr = CloudDBCache.FindCache(FileUniqueKey, Key, To);
                    if (GetStr.Length > 0)
                    {
                        if (GetStr.Equals(Result))
                        {
                            return true;
                        }
                    }

                    const string SQLOrder = "INSERT INTO LocalTranslation ([FileUniqueKey], [Key], [To], [Source], [Result], [Index]) VALUES (@FileUniqueKey, @Key, @To, @Source, @Result, @Index);";
                    int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                        SQLOrder,
                        SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                        SQLiteBuilder.Parameter("@Key", Key),
                        SQLiteBuilder.Parameter("@To", To.Code),
                        SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                        SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Result)),
                        SQLiteBuilder.Parameter("@Index", Index));
                    if (State != 0)
                    {
                        return true;
                    }
                }
                else
                {
                    const string SQLOrder = "UPDATE LocalTranslation SET [Result] = @Result, [Index] = @Index WHERE Rowid = @Rowid;";
                    int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                        SQLOrder,
                        SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Result)),
                        SQLiteBuilder.Parameter("@Index", Index),
                        SQLiteBuilder.Parameter("@Rowid", GetRowID));
                    if (State != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                DeleteCache(FileUniqueKey, Key, To);
            }

            return false;
        }

        public static bool ClearLocalCache(int FileUniqueKey)
        {
            const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey;";
            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));
            if (State != 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<LocalTranslationItem> GetItemsByFileKey(int FileUniqueKey)
        {
            const string SQLOrder = "Select * From LocalTranslation Where [FileUniqueKey] = @FileUniqueKey;";
            List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));

            List<LocalTranslationItem> LocalTransItems = new List<LocalTranslationItem>();

            for (int i = 0; i < NTable.Count; i++)
            {
                var Row = NTable[i];

                LocalTransItems.Add(new LocalTranslationItem(
                            Row["FileUniqueKey"],
                            Row["Key"],
                            P_Convert.ObjToStr(Row["To"]),
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"]))
                ));
            }

            return LocalTransItems;
        }
    }
}