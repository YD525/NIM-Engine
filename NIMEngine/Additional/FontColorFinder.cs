using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using NIMEngine.ADO;
using NIMEngine.Common;

namespace NIMEngine.Additional
{
    public class FontColorFinder
    {
        public class FontColor
        {
            public int FileUniqueKey = 0;
            public string Key = "";
            public int R = 0;
            public int G = 0;
            public int B = 0;

            public FontColor(int FileUniqueKey, string Key, int R, int G, int B)
            {
                this.FileUniqueKey = FileUniqueKey;
                this.Key = Key;
                this.R = R;
                this.G = G;
                this.B = B;
            }

            public FontColor(object FileUniqueKey, object Key, object R, object G, object B)
            {
                this.FileUniqueKey = P_Convert.ObjToInt(FileUniqueKey);
                this.Key = P_Convert.ObjToStr(Key);
                this.R = P_Convert.ObjToInt(R);
                this.G = P_Convert.ObjToInt(G);
                this.B = P_Convert.ObjToInt(B);
            }
        }
        public static void Init()
        {
            string TableName = "FontColors";
            string CreateSQL = @"CREATE TABLE [FontColors]([FileUniqueKey] INT,[Key] TEXT, [R] INT, [G] INT, [B] INT);";

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

                string[] ExpectedCols = { "FileUniqueKey", "Key", "R", "G", "B" };
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

        public static FontColor FindColor(int FileUniqueKey, string Key)
        {
            const string SQLOrder = "SELECT * FROM FontColors WHERE FileUniqueKey = @FileUniqueKey AND [Key] = @Key;";
            List<Dictionary<string, object>> NTable = NIM_Engine.LocalDB.ExecuteQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@Key", Key));
            if (NTable.Count > 0)
            {
                var Row = NTable[0]; // Dictionary<string, object>

                return new FontColor(
                    Row["FileUniqueKey"],
                    Row["Key"],
                    Row["R"],
                    Row["G"],
                    Row["B"]
                );
            }

            return null;
        }

        public static bool DeleteColor(int FileUniqueKey, string Key)
        {
            const string SQLOrder =
                "DELETE FROM FontColors WHERE FileUniqueKey = @FileUniqueKey AND [Key] = @Key;";
            int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                SQLOrder,
                SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                SQLiteBuilder.Parameter("@Key", Key));
            if (State != 0)
            {
                return true;
            }

            return false;
        }

        public static bool SetColor(int FileUniqueKey, string Key, int R, int G, int B)
        {
            if ((R == 255 && G == 255 && B == 255) == false)
            {
                int GetRowID = P_Convert.ObjToInt(NIM_Engine.LocalDB.ExecuteScalar(
                    "SELECT Rowid FROM FontColors WHERE [FileUniqueKey] = @FileUniqueKey AND [Key] = @Key;",
                    SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                    SQLiteBuilder.Parameter("@Key", Key)));

                if (GetRowID < 0)
                {
                    const string SQLOrder = @"INSERT INTO FontColors ([FileUniqueKey], [Key], [R], [G], [B]) VALUES (@FileUniqueKey, @Key, @Red, @Green, @Blue);";
                    int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                        SQLOrder,
                        SQLiteBuilder.Parameter("@FileUniqueKey", FileUniqueKey),
                        SQLiteBuilder.Parameter("@Key", Key),
                        SQLiteBuilder.Parameter("@Red", R),
                        SQLiteBuilder.Parameter("@Green", G),
                        SQLiteBuilder.Parameter("@Blue", B));
                    if (State != 0)
                    {
                        return true;
                    }
                }
                else
                {
                    const string SQLOrder = @"UPDATE FontColors SET [R] = @Red, [G] = @Green, [B] = @Blue WHERE Rowid = @Rowid;";
                    int State = NIM_Engine.LocalDB.ExecuteNonQuery(
                        SQLOrder,
                        SQLiteBuilder.Parameter("@Red", R),
                        SQLiteBuilder.Parameter("@Green", G),
                        SQLiteBuilder.Parameter("@Blue", B),
                        SQLiteBuilder.Parameter("@Rowid", GetRowID));
                    if (State != 0)
                    {
                        return true;
                    }
                }
            }
            else
            {
                DeleteColor(FileUniqueKey, Key);
            }

            return false;
        }
    }
}