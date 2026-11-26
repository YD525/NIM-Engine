using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NIM_Plugin.Data;
using NIMEngine.Engine;

namespace NIMEngine.Game
{
    public class P_ModReaderLoadResult
    {
        /// <summary>
        /// The file or directory path used as the lookup key.
        /// </summary>
        public string Key = "";

        /// <summary>
        /// Indicates whether the reader successfully loaded the file or directory.
        /// </summary>
        public bool IsSuccess = false;

        /// <summary>
        /// The path of the plugin that successfully handled the file or directory.
        /// </summary>
        public string PluginPath = "";

        public P_ModReaderLoadResult(string SetPath, bool IsSuccess, string PluginPath)
        {
            this.Key = SetPath;
            this.IsSuccess = IsSuccess;
            this.PluginPath = PluginPath;
        }

        public P_ModReaderLoadResult()
        {
            this.IsSuccess = false;
        }
    }

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
    public class P_ModReader
    {
        public static HashSet<string> SupportedFileExtensions = new HashSet<string>();
        public static HashSet<string> SupportedDirectoryStructs = new HashSet<string>();

        public static Dictionary<string, I_ModReader> CanUseReaders = new Dictionary<string, I_ModReader>();
        public static Dictionary<string, I_ModReader> CurrentReaders = new Dictionary<string, I_ModReader>(StringComparer.OrdinalIgnoreCase);

        public delegate void ModFileSaveStarting(ref string Path);
        public delegate void ModFileSaveCompleted(bool IsComplete);

        public static ModFileSaveStarting SaveStarting;
        public static ModFileSaveCompleted SaveCompleted;

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

                        foreach (var GetExt in ReaderInstance.TakenOverFileExtension.Split(','))
                        {
                            SupportedFileExtensions.Add(GetExt.ToLowerInvariant());
                        }

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

        public bool CanRead(string FileExtension)
        {
            if (SupportedFileExtensions.Contains(FileExtension))
            {
                return true;
            }

            return false;
        }

        private static List<string> GetAllFiles(string DirectoryPath)
        {
            return Directory.GetFiles(DirectoryPath, "*", SearchOption.AllDirectories).ToList();
        }

        private static List<string> GetFilesByExtensions(List<string> Files, params string[] Extensions)
        {
            List<string> Result = new List<string>();

            HashSet<string> ExtensionSet = new HashSet<string>(Extensions, StringComparer.OrdinalIgnoreCase);

            foreach (string FilePath in Files)
            {
                if (ExtensionSet.Contains(Path.GetExtension(FilePath)))
                {
                    Result.Add(FilePath);
                }
            }

            return Result;
        }

        private static List<string> GetDirectories(string DirectoryPath)
        {
            List<string> Result = new List<string>();

            Result.AddRange(Directory.GetDirectories(DirectoryPath, "*", SearchOption.AllDirectories));

            return Result;
        }

        public static P_ModReaderLoadResult Read(string SetPath,int FileUniqueKey, ref P_ModContent ContentRef)
        {
            if (string.IsNullOrEmpty(SetPath)) return new P_ModReaderLoadResult();

            SetPath = NormalizePath(SetPath);

            if (CurrentReaders.ContainsKey(SetPath)) return new P_ModReaderLoadResult();

            bool IsDirectory = Directory.Exists(SetPath);
            bool IsFile = File.Exists(SetPath);

            List<string> SetDirs = new List<string>(); 
            List<string> SetFiles = new List<string>();

            string Ext = string.Empty;

            if (IsFile)
            {
                Ext = Path.GetExtension(SetPath).ToLowerInvariant();

                if (!SupportedFileExtensions.Contains(Ext))
                {
                    return new P_ModReaderLoadResult();
                }
            }

            if (IsDirectory)
            {
                SetDirs = GetDirectories(SetPath);
                SetFiles = GetAllFiles(SetPath);
            }

            foreach (var GetReader in CanUseReaders)
            {
                if (IsFile)
                {
                    if (GetReader.Value.TakenOverFileExtension.ToLowerInvariant() != Ext)
                    {
                        continue;
                    }
                }
                if (IsDirectory)
                {
                    bool FindStruct = false;

                    foreach (var GetDirectoryName in GetReader.Value.TakenOverDirectoryNames)
                    {
                        foreach (var GetDir in SetDirs)
                        {
                            if (GetDir.Contains(GetDirectoryName))
                            {
                                FindStruct = true;
                                goto Next;
                            }
                        }
                    }

                    Next:
                    if (!FindStruct)
                    {
                        continue;
                    }
                }
               
                I_ModReader SetReader = null;

                try
                {
                    SetReader = NewReaderInstance(GetReader.Value);

                    List<Mod_File> ModFiles = new List<Mod_File>();
                    var FileExtensions = SetReader.TakenOverFileExtension.Split(',');

                    if (SetReader.CanTakeOverEntireDirectory)
                    {
                        List<string> Files = GetFilesByExtensions(SetFiles, FileExtensions);

                        foreach (var GetFile in Files)
                        {
                            ModFiles.Add(new Mod_File(
                            GetFile,
                            () =>
                            {
                                try 
                                { 
                                    byte[] FileData = File.ReadAllBytes(GetFile);
                                    return FileData;
                                }
                                catch { return null; }
                            },
                            (Data) =>
                            {
                                if (SaveStarting == null || SaveCompleted == null)
                                {
                                    throw new InvalidOperationException("SaveStarting and SaveCompleted must both be assigned before saving.");
                                }

                                string TempPath = GetFile;

                                SaveStarting.Invoke(ref TempPath);

                                bool Success = false;
                                try
                                {
                                    File.WriteAllBytes(TempPath, Data);
                                    Success = true;
                                }
                                catch { }

                                SaveCompleted.Invoke(Success);

                                return true;
                            },
                            () =>
                            {
                                UniqueKeyItem GenUniqueKeyItem = new UniqueKeyItem();
                                return UniqueKeyHelper.AddItemByReturn(ref GenUniqueKeyItem, GetFile);
                            }
                            ));
                        }
                    }
                    else
                    {
                        if (IsDirectory)
                        {
                            continue;
                        }
                        ModFiles.Add(new Mod_File(
                            SetPath,
                            () =>
                            {
                                try
                                {
                                    byte[] FileData = File.ReadAllBytes(SetPath);
                                    return FileData;
                                }
                                catch { return null; }
                            },
                            (Data) =>
                            {
                                if (SaveStarting == null || SaveCompleted == null)
                                {
                                    throw new InvalidOperationException("SaveStarting and SaveCompleted must both be assigned before saving.");
                                }

                                string TempPath = SetPath;

                                SaveStarting.Invoke(ref TempPath);

                                bool Success = false;
                                try
                                {
                                    File.WriteAllBytes(TempPath, Data);
                                    Success = true;
                                }
                                catch { }

                                SaveCompleted.Invoke(Success);

                                return true;
                            },
                            () =>
                            {
                                UniqueKeyItem GenUniqueKeyItem = new UniqueKeyItem();
                                return UniqueKeyHelper.AddItemByReturn(ref GenUniqueKeyItem, SetPath);
                            }
                            ));
                    }

                    var Content = SetReader.Read(ModFiles);

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

                    CurrentReaders.Add(SetPath, SetReader);

                    ContentRef = NewContent;

                    return new P_ModReaderLoadResult(SetPath,true, GetReader.Key);


                }
                catch
                {
                    ReleaseReader(SetReader);
                }
            }

            return new P_ModReaderLoadResult();
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

     

        public static int Save(string SetPath,P_ModContent Content)
        {
            if (string.IsNullOrEmpty(SetPath)) return -1;

            SetPath = NormalizePath(SetPath);

            I_ModReader SetReader;
            if (!CurrentReaders.TryGetValue(SetPath, out SetReader)) return -1;

            // From here on the reader is always released, whatever happens,
            // so a misbehaving plugin can never leave this file stuck as "already open".
            try
            {
                if (Content == null) return -1;

                try
                {
                    Mod_Content SetContent = new Mod_Content();

                    foreach (var GetLine in Content.Lines)
                    {
                        SetContent.Add(GetLine.Key, GetLine.Value.Data, GetLine.Value.GroupID);
                    }

                    return SetReader.Save(SetContent);
                }
                catch
                {
                    return -2;
                }
            }
            finally
            {
                Close(SetPath);
            }
        }
    }
}