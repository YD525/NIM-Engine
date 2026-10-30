using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using NIMEngine.ADO;
using NIMEngine.Language;

namespace NIMEngine.Engine.ADO
{
    public class DictionaryTrainer
    {
        public enum DataSource
        {
            Null = 0,
            MachineTranslation = 1, // CloudTranslation
            ManualTranslation = 2,  // LocalTranslation
        }

        public class DictionaryTrainingItem
        {
            /// <summary>
            /// Associated database record ID.
            /// </summary>
            public int Rowid = 0;

            /// <summary>
            /// DataSource of the data (e.g., Machine/AI [CloudTranslation Table] or Manual translation [LocalTranslation Table]).
            /// </summary>
            public DataSource DataSource = DataSource.Null;

            /// <summary>
            /// Source language.
            /// </summary>
            public Languages From = Languages.Null;

            /// <summary>
            /// Target language.
            /// </summary>
            public Languages To = Languages.Null;

            /// <summary>
            /// Original text.
            /// </summary>
            public string Original = "";

            /// <summary>
            /// Translated text.
            /// </summary>
            public string Translated = "";
        }

        public class ModTrainingSession
        {
            private Dictionary<string, object> PendingItems = new Dictionary<string, object>();

            public int CutOffset { get; private set; } = 0;

            public int From = 0;

            public ModTrainingSession(int From,int FileUniqueKey)
            {
                CollectTrainableItems(FileUniqueKey);
                this.From = From;
            }

            /// <summary>
            /// FileUniqueKey acts as the primary index for both database tables.
            /// </summary>
            /// <param name="fileUniqueKey">The unique file key.</param>
            private void CollectTrainableItems(int FileUniqueKey)
            {
                // TODO: Implement logic to filter and collect data available for training.
                //The first step is to implement retrieval methods for the two tables.

                var ManualData = LocalDBCache.GetItemsByFileKey(FileUniqueKey);
                var MachineData = CloudDBCache.GetItemsByFileKey(FileUniqueKey);

                //First, data translated manually by users should be given higher priority than data translated by AI.
                //If a user consents to saving a manually translated record to the database, and an AI-translated record for the same item already exists, the AI-translated record should be automatically removed—since one version (the manual one) has already been adopted.
                Dictionary<string, object> Data = new Dictionary<string, object>();

                CutOffset = 0;

                for (int i = 0; i < MachineData.Count; i++)
                {
                    var Item = MachineData[i];//Almost wrote a bug there—haha!

                    string GenKey = Item.Source + "_" + Item.To.ToString() + "_Machine";

                    if (!Data.ContainsKey(GenKey))
                    {
                        Data.Add(GenKey, Item);
                    }

                    CutOffset = i;
                }

                for (int i = 0; i < ManualData.Count; i++)
                {
                    var Item = ManualData[i];

                    string GenKey = Item.Source + "_" + Item.To.ToString() + "_Manual";
                    string MachineKey = Item.Source + "_" + Item.To.ToString() + "_Machine";

                    if (!Data.ContainsKey(GenKey))
                    {
                        CloudTranslationItem CacheCloudItem = null;

                        if (Data.ContainsKey(MachineKey))
                        {
                            //This record exists in the cloud-based translation history.
                            //Set
                            CacheCloudItem = (CloudTranslationItem)Data[MachineKey];
                            //Remove
                            Data.Remove(MachineKey);

                            this.CutOffset--;
                        }

                        Data.Add(GenKey, Item);

                        //Ensure that cloud translation records are assigned a lower priority than manual translation records.
                        if (CacheCloudItem != null)
                        {
                            Data.Add(MachineKey, CacheCloudItem);
                        }
                    }
                }

                this.PendingItems = Data;
            }

            /// <summary>
            /// Applies the confirmed training item to the database.
            /// </summary>
            /// <param name="item">The training item to apply.</param>
            public bool ApplyToAdvancedDictionary(string Key)
            {
                // TODO: Implement logic to write the confirmed training data to the database.

                //SQLiteBuilder.Parameter("@TargetFileName", SQLSafeCodec.Encode(Item.TargetFileName)),
                //SQLiteBuilder.Parameter("@Type", SQLSafeCodec.Encode(Item.Type)),
                //SQLiteBuilder.Parameter("@Source", SQLSafeCodec.Encode(Item.Source)),
                //SQLiteBuilder.Parameter("@Result", SQLSafeCodec.Encode(Item.Result)),
                //SQLiteBuilder.Parameter("@From", Item.From),
                //SQLiteBuilder.Parameter("@To", Item.To),
                //SQLiteBuilder.Parameter("@ExactMatch", Item.ExactMatch),
                //SQLiteBuilder.Parameter("@IgnoreCase", Item.IgnoreCase),
                //SQLiteBuilder.Parameter("@Regex", SQLSafeCodec.Encode(Item.Regex)));

                if (PendingItems.ContainsKey(Key))
                {
                    if (PendingItems[Key] is LocalTranslationItem)
                    {
                        var Item = (LocalTranslationItem)PendingItems[Key];

                        //Implement addition to the advanced dictionary

                        return AdvancedDictionary.AddItem(new AdvancedDictionaryItem() {
                             TargetFileName = "",
                             Source = Item.Source,
                             Result = Item.Result,
                             From = this.From,
                             To = Item.To,
                             ExactMatch = 1,//Exact Match only
                             IgnoreCase = 0,
                             Regex = string.Empty
                        });
                    }
                    else
                    {
                        var Item = (CloudTranslationItem)PendingItems[Key];

                        //Implement addition to the advanced dictionary

                        return AdvancedDictionary.AddItem(new AdvancedDictionaryItem()
                        {
                            TargetFileName = "",
                            Source = Item.Source,
                            Result = Item.Result,
                            From = this.From,
                            To = Item.To,
                            ExactMatch = 1,//Exact Match only
                            IgnoreCase = 0,
                            Regex = string.Empty
                        });
                    }
                }

                return false;
            }

            public void ApplyAll()
            {
                //must use `new` here to prevent multiple threads from accessing the same object.
                foreach (var GetItem in new Dictionary<string, object>(PendingItems))
                {
                    ApplyToAdvancedDictionary(GetItem.Key);
                }
            }

            /// <summary>
            /// Removes the specified training item from PendingItems.
            /// </summary>
            /// <param name="item">The training item to remove.</param>
            public void Cancel(string Key)
            {
                PendingItems.Remove(Key);
            }

            /// <summary>
            /// Gets a sub-dictionary range based on the specified DataSource using CutOffset.
            /// </summary>
            /// <param name="dataSource">The target data source range to retrieve.</param>
            /// <returns>A sliced dictionary containing only the requested range.</returns>
            public Dictionary<string, object> GetRange(DataSource DataSource)
            {
                if (DataSource == DataSource.MachineTranslation)
                {
                    // Take items from the start up to CutOffset (pure machine translations)
                    return PendingItems.Take(CutOffset).ToDictionary(X => X.Key, X => X.Value);
                }
                else if (DataSource == DataSource.ManualTranslation)
                {
                    // Returns manual translations along with their associated/paired machine items shifted to the bottom (from CutOffset onwards)
                    return PendingItems.Skip(CutOffset).ToDictionary(X => X.Key, X => X.Value);
                }

                return new Dictionary<string, object>();
            }

            /// <summary>
            /// Convert an object into a specified instance
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="Any"></param>
            /// <returns></returns>
            public T To <T>(object Any) where T : class
            {
                if (Any is T)
                { 
                   return (T)Any;
                }
            
                return null;
            }

           
        }
    }
}