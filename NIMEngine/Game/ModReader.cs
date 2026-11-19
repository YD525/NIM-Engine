using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIM_Plugin.Data;

namespace NIMEngine.Game
{
    public class P_ModString
    {
        public string Data = "";
        public int GroupID = 0;
        public P_ModString(string Data, int GroupID)
        {
            this.Data = Data;
            this.GroupID = GroupID;
        }
    }
    public class P_ModContent
    {
        public Dictionary<string, P_ModString> Lines = new Dictionary<string, P_ModString>();
    }
    public class ModReader
    {
        public static HashSet<string> SupportedFileExtensions = new HashSet<string>();

        public static Dictionary<string, I_ModReader> CanUseReaders = new Dictionary<string, I_ModReader>();

        public static Dictionary<string, I_ModReader> CurrentReaders = new Dictionary<string, I_ModReader>(StringComparer.OrdinalIgnoreCase);

        public static void Init((bool Executed, Dictionary<string, object[]> Results) PluginData)
        {
            CanUseReaders.Clear();
            SupportedFileExtensions.Clear();

            if (PluginData.Executed)
            {
                foreach (var GetReader in PluginData.Results)
                {
                    if (GetReader.Value.Length < 1)
                    {
                        continue;
                    }

                    try
                    {
                        var ReaderInstance = (I_ModReader)GetReader.Value[0];

                        SupportedFileExtensions.Add(ReaderInstance.TakenOverFileExtension.ToLowerInvariant());

                        if (!CanUseReaders.ContainsKey(GetReader.Key))
                        {
                            CanUseReaders.Add(GetReader.Key, ReaderInstance);
                        }
                    }
                    catch { }
                }
            }
        }

        // Readers keep per-file state, so every opened file gets its own instance.
        // Falls back to the shared instance if the type has no public parameterless constructor.
        private static I_ModReader NewReaderInstance(I_ModReader Template)
        {
            try
            {
                return (I_ModReader)Activator.CreateInstance(Template.GetType());
            }
            catch
            {
                return Template;
            }
        }

        public static bool Read(string FilePath, ref P_ModContent ContentRef)
        {
            if (!File.Exists(FilePath)) return false;

            if (CurrentReaders.ContainsKey(FilePath)) return false;

            string Ext = Path.GetExtension(FilePath).ToLowerInvariant();

            if (!SupportedFileExtensions.Contains(Ext))
            {
                return false;
            }

            byte[] Data = File.ReadAllBytes(FilePath);

            foreach (var GetReader in CanUseReaders)
            {
                if (GetReader.Value.TakenOverFileExtension.ToLowerInvariant() != Ext)
                {
                    continue;
                }

                try
                {
                    var SetReader = NewReaderInstance(GetReader.Value);

                    var Content = SetReader.Read(Data);

                    // Not readable by this reader: let the next reader with the same extension try
                    if (!SetReader.CanRead() || Content == null || Content.Rows == null || Content.Rows.Count == 0)
                    {
                        continue;
                    }

                    P_ModContent NewContent = new P_ModContent();

                    foreach (var GetLine in Content.Rows)
                    {
                        NewContent.Lines.Add(GetLine.Key, new P_ModString(GetLine.Data, GetLine.GroupID));
                    }

                    ContentRef = NewContent;
                    CurrentReaders.Add(FilePath, SetReader);

                    return true;
                }
                catch { }
            }

            return false;
        }
        public static bool Save(string FilePath, P_ModContent Content)
        {
            if (Content == null) return false;

            I_ModReader SetReader;
            if (!CurrentReaders.TryGetValue(FilePath, out SetReader)) return false;

            byte[] Data;

            try
            {
                Mod_Content SetContent = new Mod_Content();

                foreach (var GetLine in Content.Lines)
                {
                    SetContent.Add(GetLine.Key, GetLine.Value.Data, GetLine.Value.GroupID);
                }

                Data = SetReader.Save(SetContent);
            }
            catch
            {
                return false;
            }

            // Never touch the original file if the serializer produced nothing
            if (Data == null || Data.Length == 0) return false;

            // Last-resort safety copy (the app layer already keeps rotating ZIP backups)
            string BackupPath = FilePath + ".backup";   // xxx.esp -> xxx.esp.backup
            bool HasOriginal = File.Exists(FilePath);

            try
            {
                if (HasOriginal)
                {
                    // Back up first, overwrite any previous .backup
                    File.Copy(FilePath, BackupPath, true);
                    File.Delete(FilePath);
                }

                File.WriteAllBytes(FilePath, Data);
                return true;
            }
            catch
            {
                // Write failed: restore the original from the backup
                try
                {
                    if (HasOriginal && File.Exists(BackupPath))
                    {
                        File.Copy(BackupPath, FilePath, true);
                    }
                }
                catch { }

                return false;
            }
        }
    }
}