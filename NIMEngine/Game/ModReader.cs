using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NIM_Plugin.Data;
using NIMEngine.Engine;

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

        // Disposes a per-file reader. The shared template instances registered in
        // CanUseReaders are never disposed, other files may still be using them.
        private static void ReleaseReader(I_ModReader SetReader)
        {
            if (SetReader == null) return;

            if (CanUseReaders.ContainsValue(SetReader)) return;

            IDisposable Disposable = SetReader as IDisposable;
            if (Disposable != null)
            {
                try { Disposable.Dispose(); } catch { }
            }
        }

        private static string NormalizePath(string FilePath)
        {
            try
            {
                return Path.GetFullPath(FilePath);
            }
            catch
            {
                return FilePath;
            }
        }

        public static bool Read(string FilePath,ref P_ModContent ContentRef)
        {
            if (string.IsNullOrEmpty(FilePath)) return false;

            FilePath = NormalizePath(FilePath);

            if (!File.Exists(FilePath)) return false;

            if (CurrentReaders.ContainsKey(FilePath)) return false;

            string Ext = Path.GetExtension(FilePath).ToLowerInvariant();

            if (!SupportedFileExtensions.Contains(Ext))
            {
                return false;
            }

            byte[] Data = File.ReadAllBytes(FilePath);

            UniqueKeyItem FileKey = new UniqueKeyItem();

            int FileUniqueKey = UniqueKeyHelper.AddItemByReturn(ref FileKey, FilePath);

            foreach (var GetReader in CanUseReaders)
            {
                if (GetReader.Value.TakenOverFileExtension.ToLowerInvariant() != Ext)
                {
                    continue;
                }

                I_ModReader SetReader = null;

                try
                {
                    SetReader = NewReaderInstance(GetReader.Value);

                    var Content = SetReader.Read(FileUniqueKey,Data);

                    // Not readable by this reader: release it and let the next reader with the same extension try
                    if (!SetReader.CanRead() || Content == null || Content.Rows == null || Content.Rows.Count == 0)
                    {
                        ReleaseReader(SetReader);
                        continue;
                    }

                    P_ModContent NewContent = new P_ModContent();

                    foreach (var GetLine in Content.Rows)
                    {
                        NewContent.Lines.Add(GetLine.Key, new P_ModString(GetLine.Data, GetLine.GroupID));
                    }

                    CurrentReaders.Add(FilePath, SetReader);
                    ContentRef = NewContent;

                    return true;
                }
                catch
                {
                    ReleaseReader(SetReader);
                }
            }

            return false;
        }

        public static bool Close(string FilePath)
        {
            if (string.IsNullOrEmpty(FilePath)) return false;

            FilePath = NormalizePath(FilePath);

            I_ModReader SetReader;
            if (!CurrentReaders.TryGetValue(FilePath, out SetReader)) return false;

            CurrentReaders.Remove(FilePath);

            ReleaseReader(SetReader);

            return true;
        }

        public static bool Save(string FilePath, P_ModContent Content)
        {
            if (string.IsNullOrEmpty(FilePath)) return false;

            FilePath = NormalizePath(FilePath);

            I_ModReader SetReader;
            if (!CurrentReaders.TryGetValue(FilePath, out SetReader)) return false;

            // From here on the reader is always released, whatever happens,
            // so a misbehaving plugin can never leave this file stuck as "already open".
            try
            {
                if (Content == null) return false;

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
                string TempPath = FilePath + ".tmp";
                string BackupPath = FilePath + ".backup";   // xxx.esp -> xxx.esp.backup

                try
                {
                    // Write everything to a temp file first, the original is untouched so far
                    File.WriteAllBytes(TempPath, Data);

                    // Swap in one step; the old file becomes .backup (overwrites a previous one)
                    if (File.Exists(FilePath))
                    {
                        File.Replace(TempPath, FilePath, BackupPath);
                    }
                    else
                    {
                        File.Move(TempPath, FilePath);
                    }

                    return true;
                }
                catch
                {
                    // Any failure above leaves the original file as it was
                    try
                    {
                        if (File.Exists(TempPath)) File.Delete(TempPath);
                    }
                    catch { }

                    return false;
                }
            }
            finally
            {
                Close(FilePath);
            }
        }
    }
}