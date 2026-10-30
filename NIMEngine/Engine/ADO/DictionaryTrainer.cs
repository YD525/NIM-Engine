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
            public List<DictionaryTrainingItem> PendingItems = new List<DictionaryTrainingItem>();

            public ModTrainingSession(int FileUniqueKey)
            {
                CollectTrainableItems(FileUniqueKey);
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

            }

            /// <summary>
            /// Applies the confirmed training item to the database.
            /// </summary>
            /// <param name="item">The training item to apply.</param>
            public void ApplyToAdvancedDictionary(DictionaryTrainingItem item)
            {
                // TODO: Implement logic to write the confirmed training data to the database.
            }

            /// <summary>
            /// Removes the specified training item from PendingItems.
            /// </summary>
            /// <param name="item">The training item to remove.</param>
            public void Cancel(DictionaryTrainingItem Item)
            {
                PendingItems.Remove(Item);
            }
        }
    }
}