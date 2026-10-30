using System;
using System.Collections.Generic;
using System.Linq;
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
            private Dictionary<string, object> Manual = new Dictionary<string, object>();
            private Dictionary<string, object> Machine = new Dictionary<string, object>();


            public int From = 0;

            public ModTrainingSession(int From, int FileUniqueKey)
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
                Dictionary<string, object> TempData = new Dictionary<string, object>();

                for (int i = 0; i < MachineData.Count; i++)
                {
                    var Item = MachineData[i];//Almost wrote a bug there—haha!

                    string GenKey = Item.Source + "_" + Item.To.ToString() + "_Machine";

                    if (!TempData.ContainsKey(GenKey))
                    {
                        TempData.Add(GenKey, Item);
                    }
                }

                for (int i = 0; i < ManualData.Count; i++)
                {
                    var Item = ManualData[i];

                    string GenKey = Item.Source + "_" + Item.To.ToString() + "_Manual";
                    string MachineKey = Item.Source + "_" + Item.To.ToString() + "_Machine";

                    if (!Manual.ContainsKey(GenKey))
                    {
                        CloudTranslationItem CacheCloudItem = null;

                        if (TempData.ContainsKey(MachineKey))
                        {
                            //This record exists in the cloud-based translation history.
                            //Set
                            CacheCloudItem = (CloudTranslationItem)TempData[MachineKey];
                            //Remove
                            TempData.Remove(MachineKey);
                        }

                        Manual.Add(GenKey, Item);

                        //Ensure that cloud translation records are assigned a lower priority than manual translation records.
                        if (CacheCloudItem != null)
                        {
                            Manual.Add(MachineKey, CacheCloudItem);
                        }
                    }
                }

                this.Machine = TempData;
            }

            /// <summary>
            /// Applies the confirmed training item to the database.
            /// </summary>
            /// <param name="Key">The training item to apply.</param>
            public bool Apply(string Key)
            {
                object Value = null;

                lock (CancelLock)
                {
                    if (!Machine.TryGetValue(Key, out Value))
                    {
                        Manual.TryGetValue(Key, out Value);
                    }
                }

                string Source, Result;
                int To;

                if (Value is LocalTranslationItem)
                {
                    var Item = (LocalTranslationItem)Value;
                    Source = Item.Source; Result = Item.Result; To = Item.To;
                }
                else if (Value is CloudTranslationItem)
                {
                    var Item = (CloudTranslationItem)Value;
                    Source = Item.Source; Result = Item.Result; To = Item.To;
                }
                else
                {
                    return false;
                }

                return AdvancedDictionary.AddItem(new AdvancedDictionaryItem()
                {
                    TargetFileName = "",
                    Source = Source,
                    Result = Result,
                    From = this.From,
                    To = To,
                    ExactMatch = 1,//Exact Match only
                    IgnoreCase = 0,
                    Regex = string.Empty
                });
            }

            private object CancelLock = new object();

            /// <summary>
            /// Removes the specified training item from PendingItems.
            /// </summary>
            /// <param name="Key">The training item to remove.</param>
            public void Cancel(string Key)
            {
                lock (CancelLock)
                {
                    if (Machine.ContainsKey(Key))
                    {
                        Machine.Remove(Key);
                    }

                    if (Manual.ContainsKey(Key))
                    {
                        Manual.Remove(Key);
                    }
                }
            }

            /// <summary>
            /// Convert an object into a specified instance
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="Any"></param>
            /// <returns></returns>
            public T To<T>(object Any) where T : class
            {
                if (Any is T)
                {
                    return (T)Any;
                }

                return null;
            }

            public List<Dictionary<string, object>> GetData()
            {
                lock (CancelLock)
                    return new List<Dictionary<string, object>>
                           {
                              new Dictionary<string, object>(Manual),
                              new Dictionary<string, object>(Machine)
                           };
            }
        }
    }
}