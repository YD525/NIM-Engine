using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NIMEngine.Common;
using NIMEngine.Engine;
using NIMEngine.Language;

namespace NIMEngine.ADO
{
    public class CloudTranslationItem
    {
        public int FileUniqueKey = 0;
        public string Key = "";
        public int To = 0;
        public string Source = "";
        public string Result = "";

        public CloudTranslationItem(object FileUniqueKey, object Key, object To, object Source, object Result)
        {
            this.FileUniqueKey = P_Convert.ObjToInt(FileUniqueKey);
            this.Key = P_Convert.ObjToStr(Key);
            this.To = P_Convert.ObjToInt(To);
            this.Source = P_Convert.ObjToStr(Source);
            this.Result = P_Convert.ObjToStr(Result);
        }
    }

    public class CloudDBCache
    {
        public static void Init()
        {
            string TableName = "CloudTranslation";
            string CreateSql = @"
CREATE TABLE [CloudTranslation](
  [FileUniqueKey] INT, 
  [Key] TEXT, 
  [To] INT, 
  [Source] TEXT,
  [Result] TEXT
);";

            // Check if table exists
            const string CheckTableSql =
                "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @TableName;";
            var Result = NIM_Engine.LocalDB.ExecuteScalar(
                CheckTableSql,
                SqliteSql.Parameter("@TableName", TableName));

            if (Result != null && Result != DBNull.Value)
            {
                // Table exists, check structure
                string QuotedTableName = SqliteSql.QuoteIdentifier(TableName);
                List<Dictionary<string, object>> Columns =
                    NIM_Engine.LocalDB.ExecuteQuery("PRAGMA table_info(" + QuotedTableName + ");");
                var ExistingCols = new HashSet<string>(
                    Columns.Select(R => R["name"].ToString()),
                    StringComparer.OrdinalIgnoreCase
                );

                string[] ExpectedCols = { "FileUniqueKey", "Key", "To", "Source", "Result" };
                bool StructureChanged =
                    ExistingCols.Count != ExpectedCols.Length ||
                    ExpectedCols.Any(C => !ExistingCols.Contains(C));

                if (StructureChanged)
                {
                    NIM_Engine.LocalDB.ExecuteNonQuery("DROP TABLE IF EXISTS " + QuotedTableName + ";");
                    NIM_Engine.LocalDB.ExecuteNonQuery(CreateSql);
                }
            }
            else
            {
                // Create if not exists
                NIM_Engine.LocalDB.ExecuteNonQuery(CreateSql);
            }
        }

        public static bool DeleteCache(int FileUniqueKey, string Key, Languages TargetLanguage)
        {
            try
            {
                const string SqlOrder = @"
DELETE FROM CloudTranslation
WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey),
                    SqliteSql.Parameter("@Key", Key),
                    SqliteSql.Parameter("@To", (int)TargetLanguage));

                if (State != 0)
                {
                    return true;
                }

                return false;
            }
            catch { return false; }
        }
        public static string FindCache(int FileUniqueKey, string Key, Languages TargetLanguage)
        {
            try
            {
                const string SqlOrder = @"
SELECT Result FROM CloudTranslation
WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                string GetResult = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                    SqlOrder,
                    SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey),
                    SqliteSql.Parameter("@Key", Key),
                    SqliteSql.Parameter("@To", (int)TargetLanguage)));

                if (GetResult.Trim().Length > 0)
                {
                    return SQLSafeCodec.Decode(GetResult);
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool AddCache(int FileUniqueKey, string Key, int To, string Source, string Result)
        {
            try
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                int GetRowID = P_Convert.ObjToInt(NIM_Engine.LocalDB.ExecuteScalar(
                    @"SELECT Rowid FROM CloudTranslation
WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;",
                    SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey),
                    SqliteSql.Parameter("@Key", Key),
                    SqliteSql.Parameter("@To", To)));

                if (GetRowID <= 0)
                {
                    const string SqlOrder = @"
INSERT INTO CloudTranslation ([FileUniqueKey], [Key], [To], [Source], [Result])
VALUES (@FileUniqueKey, @Key, @To, @Source, @Result);";
                    int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                        SqlOrder,
                        SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey),
                        SqliteSql.Parameter("@Key", Key),
                        SqliteSql.Parameter("@To", To),
                        SqliteSql.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                        SqliteSql.Parameter("@Result", SQLSafeCodec.Encode(Result)));

                    if (State != 0)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch { return false; }
        }

        public static List<CloudTranslationItem> MatchCloudItem(int To, string Source, int Limit = 5)
        {
            try
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                List<CloudTranslationItem> CloudTranslationItems = new List<CloudTranslationItem>();

                const string SqlOrder = @"
SELECT * FROM CloudTranslation
WHERE [To] = @To AND [Source] = @Source
LIMIT @Limit;";
                List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@To", To),
                    SqliteSql.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SqliteSql.Parameter("@Limit", 5));
                if (NTable.Count > 0)
                {
                    for (int I = 0; I < NTable.Count; I++)
                    {
                        var Row = NTable[I];

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
            catch { }

            return new List<CloudTranslationItem>();
        }

        public static CloudTranslationItem Match(int To, string Source)
        {
            try
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                const string SqlOrder = @"
SELECT * FROM CloudTranslation
WHERE [To] = @To AND [Source] = @Source
LIMIT 1;";
                List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@To", To),
                    SqliteSql.Parameter("@Source", SQLSafeCodec.Encode(Source)));
                if (NTable.Count > 0)
                {
                    var Row = NTable[0];
                    return new CloudTranslationItem(
                        Row["FileUniqueKey"],
                        Row["Key"],
                        Row["To"],
                        SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                        SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"]))
                    );
                }

                return null;
            }
            catch { }

            return null;
        }

        public static List<CloudTranslationItem> MatchOtherCloudItem(int Rowid, int To, string Source, int Limit = 5)
        {
            try
            {
                new TranslationPreprocessor().OptimizeStrings(ref Source);

                List<CloudTranslationItem> CloudTranslationItems = new List<CloudTranslationItem>();

                const string SqlOrder = @"
SELECT * FROM CloudTranslation
WHERE [To] = @To AND [Source] = @Source AND Rowid != @Rowid
LIMIT @Limit;";
                List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@To", To),
                    SqliteSql.Parameter("@Source", SQLSafeCodec.Encode(Source)),
                    SqliteSql.Parameter("@Rowid", Rowid),
                    SqliteSql.Parameter("@Limit", 5));
                if (NTable.Count > 0)
                {
                    for (int I = 0; I < NTable.Count; I++)
                    {
                        var Row = NTable[I];

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
            catch { }

            return new List<CloudTranslationItem>();
        }


        public static string FindCacheAndID(int FileUniqueKey, string Key, int To, ref int ID)
        {
            try
            {
                const string SqlOrder = @"
SELECT Rowid, Result FROM CloudTranslation
WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key AND [To] = @To;";
                List<Dictionary<string, object>> GetResult = NIM_Engine.LocalDB.ExecuteQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey),
                    SqliteSql.Parameter("@Key", Key),
                    SqliteSql.Parameter("@To", To));

                if (GetResult.Count > 0)
                {
                    var Row = GetResult[0];
                    string GetStr = SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"]));
                    ID = P_Convert.ObjToInt(Row["Rowid"]);
                    return GetStr;
                }

                return string.Empty;
            }
            catch { return string.Empty; }
        }

        public static bool DeleteCacheByID(int Rowid)
        {
            try
            {
                const string SqlOrder = "DELETE FROM CloudTranslation WHERE Rowid = @Rowid;";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SqlOrder,
                    SqliteSql.Parameter("@Rowid", Rowid));
                if (State != 0)
                {
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        public static bool ClearCloudCache(int FileUniqueKey)
        {
            const string SqlOrder =
                "DELETE FROM CloudTranslation WHERE [FileUniqueKey] = @FileUniqueKey;";
            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SqlOrder,
                SqliteSql.Parameter("@FileUniqueKey", FileUniqueKey));
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