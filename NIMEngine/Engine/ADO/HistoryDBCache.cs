using System;
using System.Collections.Generic;
using System.Linq;
using NIMEngine.ADO;
using NIMEngine.Common;
using NIMEngine.Platform.Request;

namespace NIMEngine.Engine.ADO
{
    public class HistoryItem
    {
        public int FileUniqueKey = 0;
        public int Rowid = 0;
        public string Key = "";
        public int To = 0;
        public string CurrentText = "";
        public int IsCurrent = 0;
        public DateTime Time;
        public string RangeID = "";

        public HistoryItem(object FileUniqueKey, object Rowid, object Key, object To, object CurrentText, object IsCurrent, object Time, object RangeID)
        {
            this.FileUniqueKey = P_Convert.ObjToInt(FileUniqueKey);

            this.Rowid = P_Convert.ObjToInt(Rowid);
            this.Key = P_Convert.ObjToStr(Key);

            this.To = P_Convert.ObjToInt(To);

            this.CurrentText = P_Convert.ObjToStr(CurrentText);

            this.IsCurrent = P_Convert.ObjToInt(IsCurrent);

            this.Time = TimeHelper.TimestampToDateTime(P_Convert.ObjToLong(Time));

            this.RangeID = P_Convert.ObjToStr(RangeID);
        }

        public HistoryItem(int FileUniqueKey, string Key, int To, string CurrentText, int IsCurrent, DateTime Time, string RangeID)
        {
            this.FileUniqueKey = FileUniqueKey;

            this.Key = Key;

            this.To = To;

            this.CurrentText = CurrentText;

            this.IsCurrent = IsCurrent;

            this.Time = Time;

            this.RangeID = RangeID;
        }
    }
    public class HistoryDBCache
    {
        public static void Init()
        {
            string TableName = "RecordsHistory";

            string CreateSQL = "CREATE TABLE [RecordsHistory]([FileUniqueKey] INT, [Key] TEXT, [To] INT, [CurrentText] TEXT, [IsCurrent] INT, [Time] INT64, [RangeID] TEXT);";

            // Check if table exists
            const string CheckTableSQL = "SELECT name FROM sqlite_master WHERE type = 'table' AND name = @TableName;";
            var Result = NIM_Engine.LocalDB.ExecuteScalar(
                CheckTableSQL,
                SQLiteBuilder.Parameter("@TableName", TableName));

            if (Result != null && Result != DBNull.Value)
            {
                // Table exists, check structure
                string QuotedTableName = SQLiteBuilder.QuoteIdentifier(TableName);
                List<Dictionary<string, object>> Columns =
                    NIM_Engine.LocalDB.ExecuteQuery("PRAGMA table_info(" + QuotedTableName + ");");

                var ExistingCols = new HashSet<string>(
                    Columns.Select(R => R["name"].ToString()),
                    StringComparer.OrdinalIgnoreCase
                );

                string[] ExpectedCols =
                {
                "FileUniqueKey",
                "Key",
                "To",
                "CurrentText",
                "IsCurrent",
                "Time",
                "RangeID"
                };

                bool StructureChanged =
                    ExistingCols.Count != ExpectedCols.Length ||
                    ExpectedCols.Any(C => !ExistingCols.Contains(C));

                if (StructureChanged)
                {
                    NIM_Engine.LocalDB.ExecuteNonQuery("DROP TABLE IF EXISTS " + QuotedTableName + ";");
                    NIM_Engine.LocalDB.ExecuteNonQuery(CreateSQL);
                }
            }
            else
            {
                // Create if not exists
                NIM_Engine.LocalDB.ExecuteNonQuery(CreateSQL);
            }
        }

        private static object LockGenRangeID = new object();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "The random suffix separates local history ranges and is not a security token.")]
        public static string GenRangeID()
        {
            lock (LockGenRangeID)
            {
                return DateTime.UtcNow.Ticks + "_" + new Random(Guid.NewGuid().GetHashCode()).Next(1000, 9999);
            }
        }

        //Ctrl+Z
        public static List<int> GetPreviousIDs(int FileUniqueKey, int CurrentID)
        {
            List<int> IDs = new List<int>();

            const string SQLOrder = "SELECT rowid AS Rowid, [RangeID], [Key] FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND rowid < @CurrentId ORDER BY rowid DESC LIMIT 1;";

            var Table = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@CurrentId", CurrentID));

            if (Table.Count == 0)
                return IDs;

            string RangeID = P_Convert.ObjToStr(Table[0]["RangeID"]);

            if (string.IsNullOrEmpty(RangeID))
            {
                IDs.Add(P_Convert.ObjToInt(Table[0]["Rowid"]));
            }
            else
            {
                const string RangeSQL = "SELECT rowid AS Rowid FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND [RangeID] = @RangeId ORDER BY rowid ASC;";
                var RangeTable = NIM_Engine.LocalDB.ExecuteQuery(
                    RangeSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@RangeId", RangeID));
                for (int i = 0; i < RangeTable.Count; i++)
                {
                    var Row = RangeTable[i];
                    IDs.Add(P_Convert.ObjToInt(Row["Rowid"]));
                }
            }

            return IDs;
        }

        //Ctrl+Y
        public static List<int> GetNextIDs(int FileUniqueKey, int CurrentID)
        {
            List<int> IDs = new List<int>();

            const string SQLOrder = "SELECT rowid AS Rowid, [RangeID], [Key] FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND rowid > @CurrentId ORDER BY rowid ASC LIMIT 1;";

            var Table = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@CurrentId", CurrentID));

            if (Table.Count == 0)
                return IDs;

            string RangeID = P_Convert.ObjToStr(Table[0]["RangeID"]);

            if (string.IsNullOrEmpty(RangeID))
            {
                IDs.Add(P_Convert.ObjToInt(Table[0]["Rowid"]));
            }
            else
            {
                const string RangeSQL = "SELECT rowid AS Rowid FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND [RangeID] = @RangeId ORDER BY rowid ASC;";
                var RangeTable = NIM_Engine.LocalDB.ExecuteQuery(
                    RangeSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@RangeId", RangeID));
                for (int i = 0; i < RangeTable.Count; i++)
                {
                    var Row = RangeTable[i];
                    IDs.Add(P_Convert.ObjToInt(Row["Rowid"]));
                }
            }

            return IDs;
        }

        //Set Pointer
        public static void SelectID(int FileUniqueKey, int ID)
        {
            const string ClearSelectionSQL = "UPDATE [RecordsHistory] SET [IsCurrent] = 0 WHERE [FileUniqueKey] = @FileUniqueKey;";
            NIM_Engine.LocalDB.ExecuteNonQuery(
                ClearSelectionSQL,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));

            const string FindRangeSQL = "SELECT [RangeID] FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND rowid = @Rowid LIMIT 1;";

            string RangeID = P_Convert.ObjToStr(NIM_Engine.LocalDB.ExecuteScalar(
                FindRangeSQL,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@Rowid", ID)));

            if (!string.IsNullOrEmpty(RangeID))
            {
                const string SelectRangeSQL = "UPDATE [RecordsHistory] SET [IsCurrent] = 1 WHERE [FileUniqueKey] = @FileUniqueKey AND [RangeID] = @RangeId;";
                NIM_Engine.LocalDB.ExecuteNonQuery(
                    SelectRangeSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@RangeId", RangeID));
            }
            else
            {
                const string SelectRowSQL = "UPDATE [RecordsHistory] SET [IsCurrent] = 1 WHERE [FileUniqueKey] = @FileUniqueKey AND rowid = @Rowid;";
                NIM_Engine.LocalDB.ExecuteNonQuery(
                    SelectRowSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Rowid", ID));
            }
        }

        public static List<int> GetSelectIDs(int FileUniqueKey)
        {
            List<int> IDs = new List<int>();

            const string SQLOrder = "SELECT rowid AS Rowid FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey AND [IsCurrent] = 1;";

            var Table = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));
            for (int i = 0; i < Table.Count; i++)
            {
                var Row = Table[i];
                IDs.Add(P_Convert.ObjToInt(Row["Rowid"]));
            }

            return IDs;
        }

        //Add
        public static int AddHistory(HistoryItem Item)
        {
            const string SQLOrder = "INSERT INTO RecordsHistory (FileUniqueKey, [Key], [To], CurrentText, [Time], RangeID) VALUES (@FileUniqueKey, @Key, @To, @CurrentText, @Time, @RangeId) RETURNING rowid;";
            int Rowid = P_Convert.ObjToInt(NIM_Engine.LocalDB.ExecuteScalar(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", Item.FileUniqueKey),
                SQLiteBuilder.Parameter("@Key", Item.Key),
                SQLiteBuilder.Parameter("@To", Item.To),
                SQLiteBuilder.Parameter("@CurrentText", SQLSafeCodec.Encode(Item.CurrentText)),
                SQLiteBuilder.Parameter("@Time", TimeHelper.DateTimeToTimestamp(Item.Time)),
                SQLiteBuilder.Parameter("@RangeId", Item.RangeID)));
            return Rowid;
        }


        //Delete
        public static bool DeleteHistory(int FileUniqueKey, int ID)
        {
            const string SQLOrder = "DELETE FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey AND rowid = @Rowid;";
            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@Rowid", ID));
            return State != 0;
        }

        public static bool CheckPreviousHistoryItem(int CurrentID, int FileUniqueKey, int To, string Key, string CurrentText, out int TargetID)
        {
            int PreviousRowid = P_Convert.ObjToInt(
                NIM_Engine.LocalDB.ExecuteScalar("SELECT Rowid FROM RecordsHistory WHERE Rowid < @CurrentId ORDER BY Rowid DESC LIMIT 1;",
                SQLiteBuilder.Parameter("@CurrentId", CurrentID))
            );


            if (PreviousRowid <= 0)
            {
                TargetID = 0;
                return false;
            }

            int Count = P_Convert.ObjToInt(
                NIM_Engine.LocalDB.ExecuteScalar("SELECT COUNT(*) FROM RecordsHistory WHERE Rowid = @Rowid AND FileUniqueKey = @FileUniqueKey AND [To] = @To AND [Key] = @Key AND CurrentText = @CurrentText;",
                SQLiteBuilder.Parameter("@Rowid", PreviousRowid),
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@To", To),
                SQLiteBuilder.Parameter("@Key", Key),
                SQLiteBuilder.Parameter("@CurrentText", SQLSafeCodec.Encode(CurrentText)))
            );

            if (Count > 0)
            {
                TargetID = PreviousRowid;

                return true;
            }
            else
            {
                TargetID = 0;
                return false;
            }
        }

        //Get Full InFo By CurrentKey
        public static HistoryItem IDToHistoryItem(int FileUniqueKey, int ID)
        {
            const string SQLOrder = "SELECT rowid AS Rowid, * FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey AND rowid = @Rowid LIMIT 1;";
            var NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@Rowid", ID));

            if (NTable.Count > 0)
            {
                var Row = NTable[0];
                return new HistoryItem(
                    Row["FileUniqueKey"],
                    Row["Rowid"],
                    Row["Key"],
                    Row["To"],
                    SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["CurrentText"])),
                    Row["IsCurrent"],
                    Row["Time"],
                    Row["RangeID"]
                );
            }
            return null;
        }

        //Get HistoryItems
        public static List<HistoryItem> GetHistoryItems(int FileUniqueKey, int To)
        {
            List<HistoryItem> HistoryItems = new List<HistoryItem>();

            const string SQLOrder = "SELECT rowid AS Rowid, * FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey AND [To] = @To;";

            var NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@To", To));

            if (NTable.Count > 0)
            {
                for (int i = 0; i < NTable.Count; i++)
                {
                    var Row = NTable[i];

                    HistoryItems.Add(new HistoryItem(
                        Row["FileUniqueKey"],
                        Row["Rowid"],
                        Row["Key"],
                        Row["To"],
                        SQLSafeCodec.Decode(P_Convert.ObjToStr(Row["CurrentText"])),
                        Row["IsCurrent"],
                        Row["Time"],
                        Row["RangeID"]
                    ));
                }
            }

            return HistoryItems;
        }

        public static bool ClearHistory(int FileUniqueKey)
        {
            const string SQLOrder = "DELETE FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey;";

            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));

            if (State != 0)
            {
                return true;
            }
            return false;
        }

        // Compact history records.
        // Removes old history entries when the record count exceeds the limit.
        // RangeID records are treated as a single operation and will not be split.
        public static bool CompactHistory(int FileUniqueKey, int MaxCount = 10000)
        {
            const string CountSQL = "SELECT COUNT(*) FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey;";

            int Count = P_Convert.ObjToInt(
                NIM_Engine.LocalDB.ExecuteScalar(
                CountSQL,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey))
            );

            if (Count <= MaxCount)
                return false;

            int NeedDelete = Count - MaxCount;

            while (NeedDelete > 0)
            {
                const string FindSQL = "SELECT rowid AS Rowid, [RangeID] FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey ORDER BY rowid ASC LIMIT 1;";

                var Table = NIM_Engine.LocalDB.ExecuteQuery(
                    FindSQL,
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));

                if (Table.Count == 0)
                    break;

                string RangeID = P_Convert.ObjToStr(Table[0]["RangeID"]);
                int DeleteCount = 0;

                if (!string.IsNullOrEmpty(RangeID))
                {
                    const string DeleteRangeSQL = "DELETE FROM RecordsHistory WHERE FileUniqueKey = @FileUniqueKey AND RangeID = @RangeId;";
                    DeleteCount = NIM_Engine.LocalDB.ExecuteNonQuery(
                        DeleteRangeSQL,
                        SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                        SQLiteBuilder.Parameter("@RangeId", RangeID));
                }
                else
                {
                    int Rowid = P_Convert.ObjToInt(Table[0]["Rowid"]);
                    const string DeleteSQL = "DELETE FROM RecordsHistory WHERE rowid = @Rowid;";
                    DeleteCount = NIM_Engine.LocalDB.ExecuteNonQuery(
                        DeleteSQL,
                        SQLiteBuilder.Parameter("@Rowid", Rowid));
                }

                if (DeleteCount <= 0)
                    break;

                NeedDelete -= DeleteCount;
            }

            return true;
        }

        public static int GetLastID(int FileUniqueKey)
        {
            const string SQLOrder = "SELECT rowid AS Rowid FROM [RecordsHistory] WHERE [FileUniqueKey] = @FileUniqueKey ORDER BY rowid DESC LIMIT 1;";

            var Table = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey));

            if (Table.Count > 0)
            {
                return P_Convert.ObjToInt(Table[0]["Rowid"]);
            }

            return 0;
        }
    }
}