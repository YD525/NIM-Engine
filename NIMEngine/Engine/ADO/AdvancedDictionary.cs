using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using NIMEngine.Common;
using NIMEngine.Language;

namespace NIMEngine.ADO
{
    public class AdvancedDictionaryItem
    {
        public int Rowid = 0;
        public string TargetFileName = "";
        public string Type = "";
        public string Source = "";
        public string Result = "";
        public int From = 0;
        public int To = 0;
        public int ExactMatch = 0;
        public int IgnoreCase = 0;
        public string Regex = "";

        public AdvancedDictionaryItem()
        {

        }
        public AdvancedDictionaryItem(object TargetFileName, object Type, object Source, object Result, object From, object To, object ExactMatch, object IgnoreCase, object Regex)
        {
            this.TargetFileName = P_Convert.ObjToStr(TargetFileName);
            this.Type = P_Convert.ObjToStr(Type);
            this.Source = P_Convert.ObjToStr(Source);
            this.Result = P_Convert.ObjToStr(Result);
            this.From = P_Convert.ObjToInt(From);
            this.To = P_Convert.ObjToInt(To);
            this.ExactMatch = P_Convert.ObjToInt(ExactMatch);
            this.IgnoreCase = P_Convert.ObjToInt(IgnoreCase);
            this.Regex = P_Convert.ObjToStr(Regex);
        }
        public AdvancedDictionaryItem(object Rowid, object TargetFileName, object Type, object Source, object Result, object From, object To, object ExactMatch, object IgnoreCase, object Regex)
        {
            this.Rowid = P_Convert.ObjToInt(Rowid);
            this.TargetFileName = P_Convert.ObjToStr(TargetFileName);
            this.Type = P_Convert.ObjToStr(Type);
            this.Source = P_Convert.ObjToStr(Source);
            this.Result = P_Convert.ObjToStr(Result);
            this.From = P_Convert.ObjToInt(From);
            this.To = P_Convert.ObjToInt(To);
            this.ExactMatch = P_Convert.ObjToInt(ExactMatch);
            this.IgnoreCase = P_Convert.ObjToInt(IgnoreCase);
            this.Regex = P_Convert.ObjToStr(Regex);
        }
    }
    public class AdvancedDictionary
    {
        public static void Init()
        {
            const string CheckTableSQL = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @TableName;";
            var Result = NIM_Engine.LocalDB.ExecuteScalar(
                CheckTableSQL,
                SQLiteBuilder.Parameter("@TableName", "AdvancedDictionary"));

            if (Result == null || Result == DBNull.Value)
            {
                //If the table doesn't exist, create a new one
                CreateNewTable();
            }
            else
            {
                //Table exists, check whether it's the old structure (has TargetModName instead of TargetFileName)
                string CheckOldColumnSQL = "PRAGMA table_info(AdvancedDictionary);";
                var Dt = NIM_Engine.LocalDB.ExecuteQuery(CheckOldColumnSQL);

                bool HasTargetFileName = Dt.Any(R => R["name"].ToString() == "TargetFileName");
                bool HasTargetModName = Dt.Any(R => R["name"].ToString() == "TargetModName");

                if (!HasTargetFileName && HasTargetModName)
                {
                    //Detected old table structure, migrate data to the new structure
                    MigrateOldTable();
                }
                else if (!HasTargetFileName)
                {
                    //Table structure is broken or unknown, recreate a new one
                    RecreateNewTable();
                }
            }
        }

        private static void CreateNewTable()
        {
            string SQLOrder = "CREATE TABLE [AdvancedDictionary]([TargetFileName] TEXT, [Type] TEXT, [Source] TEXT, [Result] TEXT, [From] INT, [To] INT, [ExactMatch] INT, [IgnoreCase] INT, [Regex] TEXT);";
            NIM_Engine.LocalDB.ExecuteNonQuery(SQLOrder);
        }

        private static void MigrateOldTable()
        {
            //Rename the old table
            NIM_Engine.LocalDB.ExecuteNonQuery("ALTER TABLE AdvancedDictionary RENAME TO AdvancedDictionary_Old;");

            //Create a new table with the updated structure
            CreateNewTable();

            //Migrate data from the old table to the new table
            string SQLOrder = "INSERT INTO AdvancedDictionary (TargetFileName, Type, Source, Result, [From], [To], ExactMatch, IgnoreCase, Regex) SELECT TargetModName, Type, Source, Result, [From], [To], ExactMatch, IgnoreCase, Regex FROM AdvancedDictionary_Old;";

            NIM_Engine.LocalDB.ExecuteNonQuery(SQLOrder);

            //Drop the old table after migration
            NIM_Engine.LocalDB.ExecuteNonQuery("DROP TABLE AdvancedDictionary_Old;");
        }

        private static void RecreateNewTable()
        {
            //Defensive fallback: drop the broken table and recreate it
            NIM_Engine.LocalDB.ExecuteNonQuery("DROP TABLE IF EXISTS AdvancedDictionary;");
            CreateNewTable();
        }

        public static string GetSourceByRowid(int Rowid)
        {
            const string SQLOrder = "SELECT [Source] FROM AdvancedDictionary WHERE Rowid = @Rowid;";
            return SQLSafeCodec.Decode(P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                SQLOrder,
                SQLiteBuilder.Parameter("@Rowid", Rowid))));
        }
        public static bool IsRegexMatch(string Input, string SetRegex)
        {
            try
            {
                return Regex.IsMatch(Input, SetRegex);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static AdvancedDictionaryItem ExactMatch(Languages From, Languages To, string Type, string Source)
        {
            const string SQLOrder = "SELECT Rowid, * FROM AdvancedDictionary WHERE [From] = @From AND [To] = @To AND ([Type] IS NULL OR [Type] = '' OR [Type] = @Type) AND [Source] = @Source LIMIT 1;";

            List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@From", (int)From),
                SQLiteBuilder.Parameter("@To", (int)To),
                SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Type)),
                SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Source)));
            if (NTable.Count > 0)
            {
                var Row = NTable[0]; // row is Dictionary<string, object>

                return new AdvancedDictionaryItem(
                    Row["Rowid"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["TargetFileName"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Type"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"])),
                    Row["From"],
                    Row["To"],
                    Row["ExactMatch"],
                    Row["IgnoreCase"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Regex"]))
                );
            }

            return null;
        }

        public static void KeepLongestMatches(string SourceText, ref List<AdvancedDictionaryItem> Items)
        {
            if (Items == null || Items.Count <= 1) return;

            var SortedItems = Items.OrderByDescending(X => X.Source.Length).ToList();
            var FinalList = new List<AdvancedDictionaryItem>();

            for (int i = 0; i < SortedItems.Count; i++)
            {
                var Current = SortedItems[i];
                string CurrentSource = Current.Source;

                StringComparison ComparisonMode = Current.IgnoreCase == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

                int MatchStart = SourceText.IndexOf(CurrentSource, ComparisonMode);
                if (MatchStart < 0) continue;

                int MatchEnd = MatchStart + CurrentSource.Length;

                bool Overlaps = FinalList.Any(Item =>
                {
                    StringComparison ItemComparison = Item.IgnoreCase == 1 ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                    int ItemStart = SourceText.IndexOf(Item.Source, ItemComparison);
                    int ItemEnd = ItemStart + Item.Source.Length;
                    return MatchStart < ItemEnd && ItemStart < MatchEnd;
                });

                if (!Overlaps)
                {
                    FinalList.Add(Current);
                }
            }

            Items = FinalList;
        }

        public static List<AdvancedDictionaryItem> Query(string FileName, string Type, Languages From, Languages To, string SourceText, bool UseWordBoundary)
        {
            List<AdvancedDictionaryItem> AdvancedDictionaryItems = new List<AdvancedDictionaryItem>();

            string SQLOrder = "";

            if (!UseWordBoundary)
            {
                SQLOrder = "SELECT Rowid,* FROM AdvancedDictionary WHERE (TargetFileName IS NULL OR TargetFileName = '' OR TargetFileName = @FileName) AND ([Type] IS NULL OR [Type] = '' OR [Type] = @Type) AND [From] = @From AND [To] = @To AND ((ExactMatch = 1 AND ((IgnoreCase = 1 AND LOWER(Source) = LOWER(@SourceText)) OR (IgnoreCase = 0 AND Source = @SourceText))) OR (ExactMatch = 0 AND ((IgnoreCase = 1 AND LOWER(@SourceText) LIKE '%' || LOWER(Source) || '%') OR (IgnoreCase = 0 AND @SourceText LIKE '%' || Source || '%'))));";
            }
            else
            {
                SQLOrder = "SELECT Rowid,* FROM AdvancedDictionary WHERE (TargetFileName IS NULL OR TargetFileName = '' OR TargetFileName = @FileName) AND ([Type] IS NULL OR [Type] = '' OR [Type] = @Type) AND [From] = @From AND [To] = @To AND ((ExactMatch = 1 AND ((IgnoreCase = 1 AND LOWER(Source) = LOWER(@SourceText)) OR (IgnoreCase = 0 AND Source = @SourceText))) OR (ExactMatch = 0 AND ((IgnoreCase = 1 AND INSTR(LOWER(@SourceText), LOWER(Source)) > 0 AND (INSTR(LOWER(@SourceText), LOWER(Source)) = 1 OR SUBSTR(LOWER(@SourceText), INSTR(LOWER(@SourceText), LOWER(Source)) - 1, 1) NOT GLOB '[a-z0-9_]') AND (INSTR(LOWER(@SourceText), LOWER(Source)) + LENGTH(Source) - 1 = LENGTH(@SourceText) OR SUBSTR(LOWER(@SourceText), INSTR(LOWER(@SourceText), LOWER(Source)) + LENGTH(Source), 1) NOT GLOB '[a-z0-9_]')) OR (IgnoreCase = 0 AND INSTR(@SourceText, Source) > 0 AND (INSTR(@SourceText, Source) = 1 OR SUBSTR(@SourceText, INSTR(@SourceText, Source) - 1, 1) NOT GLOB '[A-Za-z0-9_]') AND (INSTR(@SourceText, Source) + LENGTH(Source) - 1 = LENGTH(@SourceText) OR SUBSTR(@SourceText, INSTR(@SourceText, Source) + LENGTH(Source), 1) NOT GLOB '[A-Za-z0-9_]')))));";
            }

            List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileName", SQLSafeCodec.Encode(FileName)),
                SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Type)),
                SQLiteBuilder.Parameter("@From", (int)From),
                SQLiteBuilder.Parameter("@To", (int)To),
                SQLiteBuilder.Parameter("@SourceText", SQLSafeCodec.Encode(SourceText)));

            for (int i = 0; i < NTable.Count; i++)
            {
                var Row = NTable[i];
                var Get = new AdvancedDictionaryItem(
                Row["Rowid"],
                SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["TargetFileName"])),
                SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Type"])),
                SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"])),
                Row["From"],
                Row["To"],
                Row["ExactMatch"],
                Row["IgnoreCase"],
                SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Regex"]))
            );
                if (Get.Regex.Trim().Length > 0)
                {
                    if (IsRegexMatch(SourceText, System.Web.HttpUtility.HtmlDecode(Get.Regex)))
                    {
                        AdvancedDictionaryItems.Add(Get);
                    }
                }
                else
                {
                    AdvancedDictionaryItems.Add(Get);
                }
            }

            KeepLongestMatches(SourceText, ref AdvancedDictionaryItems);

            return AdvancedDictionaryItems;
        }

        public static bool CheckSame(AdvancedDictionaryItem Item)
        {
            const string CheckSQL = "SELECT COUNT(*) FROM AdvancedDictionary WHERE [TargetFileName] = @TargetFileName AND [Type] = @Type AND [Source] = @Source AND [Result] = @Result AND [From] = @From AND [To] = @To;";

            int Count = Convert.ToInt32(NIM_Engine.LocalDB.ExecuteScalar(
                CheckSQL,
                SQLiteBuilder.Parameter("@TargetFileName", SQLSafeCodec.Encode(Item.TargetFileName)),
                SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Item.Type)),
                SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Item.Source)),
                SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Item.Result)),
                SQLiteBuilder.Parameter("@From", Item.From),
                SQLiteBuilder.Parameter("@To", Item.To)));
            return Count > 0;
        }


        public static bool AddItem(AdvancedDictionaryItem Item)
        {
            if (!CheckSame(Item))
            {
                const string SQLOrder = "INSERT INTO AdvancedDictionary ([TargetFileName], [Type], [Source], [Result], [From], [To], [ExactMatch], [IgnoreCase], [Regex]) VALUES (@TargetFileName, @Type, @Source, @Result, @From, @To, @ExactMatch, @IgnoreCase, @Regex);";
                int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                    SQLOrder,
                    SQLiteBuilder.Parameter("@TargetFileName", SQLSafeCodec.Encode(Item.TargetFileName)),
                    SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Item.Type)),
                    SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Item.Source)),
                    SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Item.Result)),
                    SQLiteBuilder.Parameter("@From", Item.From),
                    SQLiteBuilder.Parameter("@To", Item.To),
                    SQLiteBuilder.Parameter("@ExactMatch", Item.ExactMatch),
                    SQLiteBuilder.Parameter("@IgnoreCase", Item.IgnoreCase),
                    SQLiteBuilder.Parameter("@Regex", SQLSafeCodec.Encode(Item.Regex)));
                if (State != 0)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return false;
            }
        }

        public static void DeleteItem(AdvancedDictionaryItem Item)
        {
            const string SQLOrder = "DELETE FROM AdvancedDictionary WHERE TargetFileName = @TargetFileName AND Type = @Type AND Source = @Source AND Result = @Result AND [From] = @From AND [To] = @To AND ExactMatch = @ExactMatch AND IgnoreCase = @IgnoreCase AND Regex = @Regex;";
            NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@TargetFileName", SQLSafeCodec.Encode(Item.TargetFileName)),
                SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Item.Type)),
                SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Item.Source)),
                SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Item.Result)),
                SQLiteBuilder.Parameter("@From", Item.From),
                SQLiteBuilder.Parameter("@To", Item.To),
                SQLiteBuilder.Parameter("@ExactMatch", Item.ExactMatch),
                SQLiteBuilder.Parameter("@IgnoreCase", Item.IgnoreCase),
                SQLiteBuilder.Parameter("@Regex", SQLSafeCodec.Encode(Item.Regex)));
        }

        public static P_SQL_Page<List<AdvancedDictionaryItem>> QueryByPage(int From, int To, int PageNo)
        {
            const string Where = SQLiteBuilder.LanguageFilter;

            int MaxPage = P_SQL_Pagination.GetPageCount(
                "AdvancedDictionary",
                Where,
                SQLiteBuilder.Parameter("@From", From),
                SQLiteBuilder.Parameter("@To", To));

            List<Dictionary<string, object>> NTable = P_SQL_Pagination.GetTablePageData(
                "AdvancedDictionary",
                PageNo,
                NIM_Engine.Config.DefPageSize,
                Where,
                SQLiteBuilder.Parameter("@From", From),
                SQLiteBuilder.Parameter("@To", To));

            List<AdvancedDictionaryItem> Items = new List<AdvancedDictionaryItem>();
            for (int i = 0; i < NTable.Count; i++)
            {
                var Row = NTable[i]; // row is Dictionary<string, object>

                Items.Add(new AdvancedDictionaryItem(
                    Row["Rowid"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["TargetFileName"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Type"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"])),
                    Row["From"],
                    Row["To"],
                    Row["ExactMatch"],
                    Row["IgnoreCase"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Regex"]))
                ));
            }

            return new P_SQL_Page<List<AdvancedDictionaryItem>>(Items, PageNo, MaxPage);
        }

        public static P_SQL_Page<List<AdvancedDictionaryItem>> QueryByPage(string SourceText, int From, int To, int PageNo)
        {
            const string Where = SQLiteBuilder.SourceLanguageFilter;

            int MaxPage = P_SQL_Pagination.GetPageCount(
                "AdvancedDictionary",
                Where,
                SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(SourceText)),
                SQLiteBuilder.Parameter("@From", From),
                SQLiteBuilder.Parameter("@To", To));

            List<Dictionary<string, object>> NTable = P_SQL_Pagination.GetTablePageData(
                "AdvancedDictionary",
                PageNo,
                NIM_Engine.Config.DefPageSize,
                Where,
                SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(SourceText)),
                SQLiteBuilder.Parameter("@From", From),
                SQLiteBuilder.Parameter("@To", To));

            List<AdvancedDictionaryItem> Items = new List<AdvancedDictionaryItem>();
            for (int I = 0; I < NTable.Count; I++)
            {
                var Row = NTable[I];

                Items.Add(new AdvancedDictionaryItem(
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["TargetFileName"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Type"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Source"])),
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Result"])),
                    Row["From"],
                    Row["To"],
                    Row["ExactMatch"],
                    Row["IgnoreCase"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["Regex"]))
                ));
            }

            return new P_SQL_Page<List<AdvancedDictionaryItem>>(Items, PageNo, MaxPage);
        }

        public static bool DeleteByRowid(int Rowid)
        {
            const string SQLOrder = "DELETE FROM AdvancedDictionary WHERE Rowid = @Rowid;";
            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@Rowid", Rowid));
            if (State != 0)
            {
                return true;
            }
            return false;
        }

    }
}