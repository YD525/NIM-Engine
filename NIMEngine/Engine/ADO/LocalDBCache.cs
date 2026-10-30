using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NIMEngine.Common;
using NIMEngine.Engine;
using NIMEngine.Language;

namespace NIMEngine.ADO
{
    public class LocalTransItem
    {
        public int FileUniqueKey = 0;
        public string Key = "";
        public int To = 0;
        public string Result = "";
        public int Index = 0;

        public LocalTransItem(int FileUniqueKey, string Key, Languages TargetLanguage, string Result)
        {
            this.FileUniqueKey = FileUniqueKey;
            this.Key = Key;
            this.To = (int)TargetLanguage;
            this.Result = Result;
            this.Index = 0;
        }

        public LocalTransItem(object FileUniqueKey, object Key, object To, object Result)
        {
            this.FileUniqueKey = P_Convert.ObjToInt(FileUniqueKey);
            this.Key = P_Convert.ObjToStr(Key);
            this.To = P_Convert.ObjToInt(To);
            this.Result = P_Convert.ObjToStr(Result);
            this.Index = 0;
        }
    }
    public class LocalDBCache
    {
        public static void Init()
        {
            string TableName = "LocalTranslation";
            string CreateSQL = "CREATE TABLE [LocalTranslation]([FileUniqueKey] INT, [Key] TEXT, [To] INT, [Source] TEXT, [Result] TEXT, [Index] INT);";

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
            }
            else
            {
                // Create if not exists
                NIM_Engine.LocalDB.ExecuteNonQuery(CreateSQL);
            }
        }

        public static List<CloudTranslationItem> MatchLocalItem(int To, string Source, int Limit = 5)
        {
            new TranslationPreprocessor().OptimizeStrings(ref Source);

            try
            {
                List<CloudTranslationItem> CloudTranslationItems = new List<CloudTranslationItem>();

                const string SQLOrder = "SELECT * FROM LocalTranslation WHERE [To] = @To AND [Source] = @Source LIMIT @Limit;";
                List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@To", To),
                    SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SQLiteBuilder.Parameter("@Limit", Limit));
                if (NTable.Count > 0)
                {
                    for (int i = 0; i < NTable.Count; i++)
                    {
                        var Row = NTable[i];

                        CloudTranslationItems.Add(new CloudTranslationItem(
                            Row["FileUniqueKey"],
                            Row["Key"],
                            Row["To"],
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                            SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"]))
                        ));
                    }
                }

                return CloudTranslationItems;
            }
            catch
            {
                return new List<CloudTranslationItem>();
            }
        }

        public static bool DeleteCacheByFileUniqueKey(int FileUniqueKey, Languages TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@To", (int)TargetLanguage));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static bool DeleteCacheBySource(int FileUniqueKey, string Source, Languages TargetLanguage)
        {
            new TranslationPreprocessor().OptimizeStrings(ref Source);

            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Source] = @Source AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SQLiteBuilder.Parameter("@To", (int)TargetLanguage));

                if (State != 0)
                {
                    return true;
                }

                return false;

            }
            catch { return false; }
        }

        public static bool DeleteCacheByResult(int FileUniqueKey, string ResultText, Languages TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Result] = @Result AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(ResultText)),
                    SQLiteBuilder.Parameter("@To", (int)TargetLanguage));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static bool DeleteCache(int FileUniqueKey, string Key, Languages TargetLanguage)
        {
            try
            {
                const string SQLOrder = "DELETE FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", (int)TargetLanguage));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }

        public static string GetCacheText(int FileUniqueKey, string Key, Languages TargetLanguage)
        {
            try
            {
                const string SQLOrder = "SELECT Result FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                string GetText = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", (int)TargetLanguage)));

                if (GetText.Trim().Length > 0)
                {
                    return SQLSafeCodec.Decode(GetText);
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static string FindCache(int FileUniqueKey, string Key, Languages TargetLanguage)
        {
            return FindCache(FileUniqueKey, Key, (int)TargetLanguage);
        }


        public static string FindCache(int FileUniqueKey, string Key, int To)
        {
            try
            {
                const string SQLOrder = "SELECT Result FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                string GetResult = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", To)));

                if (GetResult.Trim().Length > 0)
                {
                    return SQLSafeCodec.Decode(GetResult);
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool UpdateLocalTransItem(int FileUniqueKey, string Key, int To, string Source, string Result, int Index)
        {
            if (Result.Length > 0)
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                int GetRowID = P_Convert.ObjToInt(NIM_Engine.LocalDB.ExecuteScalar("SELECT Rowid FROM LocalTranslation WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;",
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key),
                    SQLiteBuilder.Parameter("@To", To)));

                if (GetRowID <= 0)
                {
                    var GetStr = CloudDBCache.FindCache(FileUniqueKey, Key, (Languages)To);
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
                        SQLiteBuilder.Parameter("@To", To),
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
                DeleteCache(FileUniqueKey, Key, (Languages)To);
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
    }
}