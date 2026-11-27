using NIMEngine.Engine;
using NIMEngine.Events;
using NIMEngine.Language;
using NIMEngine.P_Delegate;
using NIMEngine.Platform;
using NIMEngine.Sequence;
using NIMEngine.Translate;
using NIMEngine.Unit;
using NIMEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;
using System;

public class EngineCore
{
    public static readonly EngineCore Instance = new EngineCore();

    private readonly object _Lock = new object();

    private readonly Dictionary<string, int> _FailureCounts = new Dictionary<string, int>();
    private readonly Dictionary<string, bool> _DisabledPlatforms = new Dictionary<string, bool>();

    internal static void Delay(CancellationToken cancellationToken, int milliseconds)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (milliseconds <= 0)
        {
            return;
        }
        if (cancellationToken.WaitHandle.WaitOne(milliseconds))
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    public List<EngineNode> EngineNodes = new List<EngineNode>();

    public void Init()
    {
        ReloadEngine();
    }

    public void RemoveEngine<T>()
    {
        lock (_Lock)
        {
            EngineNodes.RemoveAll(e => e is T);
        }
    }

    public void ResetEngineHealth()
    {
        lock (_Lock)
        {
            _FailureCounts.Clear();
            _DisabledPlatforms.Clear();

            foreach (var Node in EngineNodes)
            {
                Node.Disabled = false;
                Node.EffectiveWeight = Node.MaxCallCount;
                Node.CurrentWeight = 0;
            }
        }
    }

    private void SyncHealthFromMap_NoLock(EngineNode Node)
    {
        Node.Disabled = _DisabledPlatforms.TryGetValue(Node.Key, out var Dis) && Dis;
    }

    internal void PreDeductWeight(EngineNode Node)
    {
        lock (_Lock)
        {
            if (Node.EffectiveWeight > 0) Node.EffectiveWeight--;
        }
    }

    internal void RegisterFailure(EngineNode Node)
    {
        lock (_Lock)
        {
            _FailureCounts.TryGetValue(Node.Key, out int Count);
            Count++;
            _FailureCounts[Node.Key] = Count;

            if (Count >= NIMEngine.NIM_Engine.Config.FailThreshold)
            {
                _DisabledPlatforms[Node.Key] = true;
                Node.Disabled = true;
            }
        }
    }

    internal void RegisterSuccess(EngineNode Node)
    {
        lock (_Lock)
        {
            _FailureCounts[Node.Key] = 0;
            if (Node.EffectiveWeight < Node.MaxCallCount) Node.EffectiveWeight++;
        }
    }

    public void ReloadEngine()
    {
        lock (_Lock)
        {
            EngineNodes.Clear();

            var Nodes = PlatformHub.GetAllNode();

            if (Nodes != null)
            {
                foreach (var GetNode in Nodes)
                {
                    if (PlatformHub.IsEnable(GetNode.Value.Name))
                    {
                        string Key = GetNode.Value.Name;
                        if (GetNode.Value.Type == 0)
                        {
                            PlatformHub.GetLocalAINode(Key)?.Init();
                        }

                        PlatformApiKeys KeyData = NIMEngine.NIM_Engine.KeyData.GetData(Key);

                        int Weight = GetNode.Value.Type == 0 ? 1 : KeyData.GetKeyCount();

                        AddNode_NoLock(new EngineNode(this, GetNode.Value.Name, GetNode.Value.Type, Weight, Key));
                    }
                }
            }
        }
    }

    private void AddNode_NoLock(EngineNode Node)
    {
        SyncHealthFromMap_NoLock(Node);
        EngineNodes.Add(Node);
    }

    public static void CheckCanSkip(string TranslatorID, Dictionary<string, UnitSequence> Sequences, ref UnitGroup Item)
    {
        foreach (var GetSeq in new Dictionary<string, UnitSequence>(Sequences))
        {
            if (GetSeq.Value.CanSkip)
            {
                foreach (var GetBaseUnit in Item.Units)
                {
                    if (GetBaseUnit.Key.Equals(GetSeq.Key))
                    {
                        if (GetBaseUnit.ApplyStateChange(TranslatorID, UnitTranslationState.Skipped).ControlSignal.Sign > 0)
                        {
                            Sequences[GetSeq.Key].CanSkip = false;
                            continue;
                        }
                    }
                }
            }
        }
    }

    private EngineNode PickNext()
    {
        lock (_Lock)
        {
            EngineNode Best = null;
            int Total = 0;

            foreach (var Node in EngineNodes)
            {
                if (Node.Disabled) continue;

                Node.CurrentWeight += Node.EffectiveWeight;
                Total += Node.EffectiveWeight;

                if (Best == null || Node.CurrentWeight > Best.CurrentWeight)
                {
                    Best = Node;
                }
            }

            if (Best != null)
            {
                Best.CurrentWeight -= Total;
            }

            return Best;
        }
    }

    public UnitGroup CallOnce(CancellationToken CancelToken, Translator TranslatorRef, TranslationPreprocessor Preprocessor, UnitGroup Item,
        P_Language From, P_Language To, string AIParam, bool CanSleep, bool UseAIMemory, bool CanUpdate, ref string UseModel)
    {
        if (!Item.ApplyStateChange(TranslatorRef.ID, UnitTranslationState.Preparing).CanDo(-1))
        {
            return Item;
        }

        Item.StartPreProcess(Preprocessor, From, To, out Dictionary<string, UnitSequence> Sequences);

        CheckCanSkip(TranslatorRef.ID, Sequences, ref Item);

        Item.CenterPreProcess(TranslatorRef, From, To, ref Sequences);

        CheckCanSkip(TranslatorRef.ID, Sequences, ref Item);

        EngineNode CurrentEngine = null;
        int IdleSpins = 0;

        while (CurrentEngine == null)
        {
            CancelToken.ThrowIfCancellationRequested();

            CurrentEngine = PickNext();

            if (CurrentEngine != null)
            {
                int MaxTry = 10;

            NextCall:

                string GetTrans = "";

                if (!Item.ApplyStateChange(TranslatorRef.ID, UnitTranslationState.Translating).CanDo(-1))
                {
                    return Item;
                }

                GetTrans = CurrentEngine.Call(TranslatorRef.ID, CancelToken, TranslatorRef, ref Item, ref Sequences, From, To,
                true, NIMEngine.NIM_Engine.Config.ContextLimit, AIParam, ref UseModel);

                CancelToken.ThrowIfCancellationRequested();

                try
                {
                    if (Preprocessor.HasUnicodeEscape(GetTrans))
                    {
                        GetTrans = Regex.Unescape(GetTrans);
                    }
                }
                catch
                {
                    goto NextCall;
                }

                int Hits = 0;
                foreach (var GetSeq in new Dictionary<string, UnitSequence>(Sequences))
                {
                    if (GetSeq.Value.CanSkipSleep)
                    {
                        Hits++;
                    }
                }

                if (CanSleep && ((Hits == Sequences.Count) == false))
                {
                    CurrentEngine.BeginSleep(CancelToken);
                }

                ConfirmPasser Passer = Item.AnalysisContent(GetTrans);

                List<BaseUnit> NotPassUnits = new List<BaseUnit>();
                List<BaseUnit> PassUnits = new List<BaseUnit>();

                bool IsDeepL = CurrentEngine.Name.Equals("DeepL");

                bool Passed = Passer.TryPass(ref NotPassUnits, ref PassUnits, IsDeepL);

                if (!Passed)
                {
                    Delay(CancelToken, NIMEngine.NIM_Engine.Config.ThrottleDelayMs);

                    if (PassUnits.Count == 0)
                    {
                        Delay(CancelToken, 1000);
                    }

                    if (MaxTry > 0)
                    {
                        MaxTry--;
                        goto NextCall;
                    }
                }

                Passer.Apply(PassUnits);

                for (int i = 0; i < PassUnits.Count; i++)
                {
                    var PassUnit = PassUnits[i];
                    Sequences[PassUnit.Key].CanSkip = true;
                    Sequences[PassUnit.Key].Step = 6;
                    Sequences[PassUnit.Key].Data = PassUnit.Translated;
                }

                Item.EndPreProcess(From, To, ref Sequences);
                Item.EndGeneratePlaceholder(From, To, ref Sequences);

                if (UseAIMemory)
                {
                    Item.UpdateAIMemory(TranslatorRef, Sequences);
                }

                if (CanUpdate)
                {
                    Item.UpdateCloudData(TranslatorRef, Sequences, new List<string>() { UseModel });
                }

                if (!Item.ApplyStateChange(TranslatorRef.ID, UnitTranslationState.Completed).CanDo(-1))
                {
                    return Item;
                }

                return Item;
            }

            IdleSpins++;
            if (IdleSpins > 500)
            {
                return null;
            }

            Delay(CancelToken, 1);
            ReloadEngine();

            if (EngineNodes.Count == 0)
            {
                return null;
            }
        }

        return null;
    }

    public class EngineNode
    {
        private readonly EngineCore Owner;

        public string Name;
        public int Type = 0;

        public string Key;
        public int MaxCallCount;
        public int EffectiveWeight;
        public int CurrentWeight;
        public bool Disabled;
        public int SleepBySec;

        public EngineNode(EngineCore Owner, string Name, int Type, int MaxCallCount, string Key) : this(Owner, Name, Type, MaxCallCount, Key, 1) { }

        public EngineNode(EngineCore Owner, string Name, int Type, int MaxCallCount, string Key, int SleepBySec)
        {
            this.Owner = Owner;
            this.Key = Key;
            this.Name = Name;
            this.Type = Type;
            this.MaxCallCount = MaxCallCount;
            this.EffectiveWeight = MaxCallCount;
            this.CurrentWeight = 0;
            this.SleepBySec = SleepBySec;
        }

        public void BeginSleep()
        {
            BeginSleep(CancellationToken.None);
        }

        public void BeginSleep(CancellationToken cancellationToken)
        {
            for (int i = 0; i < SleepBySec; i++)
            {
                Delay(cancellationToken, 1000);
            }
        }

        public static bool IsLocked(object Obj)
        {
            bool LockTaken = false;
            Monitor.TryEnter(Obj, 0, ref LockTaken);
            if (LockTaken) Monitor.Exit(Obj);
            return !LockTaken;
        }

        public string Call(string TranslatorID, CancellationToken CancelToken, Translator TranslatorRef, ref UnitGroup Source, ref Dictionary<string, UnitSequence> Sequences,
           P_Language From, P_Language To, bool UseAIMemory, int AIMemoryQueryLimit, string AIParam, ref string UseModel)
        {
            bool ForceReplace = true;
            bool ForceReplaceProtected = true;

            if (this.Type <= 2)
            {
                ForceReplace = false;
                ForceReplaceProtected = false;
            }

            Source.StartGeneratePlaceholder(TranslatorRef, From, To, ref Sequences, ForceReplace, ForceReplaceProtected);

            CheckCanSkip(TranslatorID, Sequences, ref Source);

            bool CanTrans = false;
            string GetSource = Source.GenContent(ref CanTrans);
            if (!CanTrans)
            {
                return "<empty>";
            }

            if (GetSource.Length == 0)
            {
                return string.Empty;
            }

            if (From == P_Language.Auto)
            {
                From = P_Language.Detect(GetSource,LanguageDetectMode.Line);
            }

            int MaxTranslationAttempts = NIMEngine.NIM_Engine.Config.MaxTranslationAttempts;
            string TransText = string.Empty;

            List<ReplaceTag> CustomWords = new List<ReplaceTag>();
            foreach (var GetSeq in new Dictionary<string, UnitSequence>(Sequences))
            {
                if (GetSeq.Value.HasPlaceholder)
                    CustomWords.AddRange(GetSeq.Value.Preprocessor.ReplaceTags);
            }

            bool ShouldRefund = false;
            Owner.PreDeductWeight(this);

            try
            {
                if (this.Type == 3)
                {
                    if (PlatformHub.IsEnable(this.Name))
                    {
                        PlatformCall Call = new PlatformCall();
                        bool Passed = false;
                        int MaxTry = MaxTranslationAttempts;

                        string ApiKey = "";
                        string GetData = null;

                        do
                        {
                            CancelToken.ThrowIfCancellationRequested();

                            ApiKey = NIMEngine.NIM_Engine.KeyData.GetData(this.Name).GetFirstKey();

                            bool State = PlatformHub.CallNode(
                                this.Name,
                                ApiKey,
                                NIM_Engine.Config,
                                Source,
                                From,
                                To,
                                ref Call,
                                out string TranslateStr);

                            GetData = TranslateStr.Trim();

                            Passed = TranslatorRef.Preprocessor.SecondaryQualityInspection(GetData, CustomWords);

                            if (!Passed && MaxTry > 0)
                            {
                                Delay(CancelToken, NIMEngine.NIM_Engine.Config.ReTryWaitTime);
                                MaxTry--;
                            }
                            else
                            {
                                break;
                            }
                        } while (!Passed);

                        if (GetData.Length == 0) NIMEngine.NIM_Engine.KeyData.GetData(this.Name).ReportError(ApiKey);

                        TransText = GetData;
                        Call.Output();

                        if (GetData.Trim().Length == 0 || !Passed) Owner.RegisterFailure(this); else Owner.RegisterSuccess(this);
                    }
                    else
                    {
                        ShouldRefund = true;
                    }
                }
                else if (this.Type == 0)
                {
                    if (PlatformHub.IsEnable(this.Name))
                    {
                        var CurrentNode = PlatformHub.GetLocalAINode(this.Name);

                        if (IsLocked(CurrentNode.SingleLock))
                        {
                            while (IsLocked(CurrentNode.SingleLock))
                            {
                                CancelToken.ThrowIfCancellationRequested();
                                Delay(CancelToken, 200);
                            }
                        }

                        AICall Call = new AICall();
                        string GetData = null;
                        bool Passed = false;
                        int MaxTry = MaxTranslationAttempts;

                        do
                        {
                            CancelToken.ThrowIfCancellationRequested();

                            bool State = PlatformHub.CallLocalAINode(
                                this.Name,
                                NIM_Engine.Config,
                                CustomWords,
                                Source,
                                From,
                                To,
                                UseAIMemory,
                                AIMemoryQueryLimit,
                                AIParam,
                                ref UseModel,
                                ref Call,
                                out string TranslateStr);

                            GetData = TranslateStr.Trim();

                            Passed = TranslatorRef.Preprocessor.SecondaryQualityInspection(GetData, CustomWords);

                            if (!Passed && MaxTry > 0)
                            {
                                Delay(CancelToken, NIMEngine.NIM_Engine.Config.ReTryWaitTime);
                                MaxTry--;
                            }
                            else
                            {
                                break;
                            }
                        } while (!Passed);

                        TransText = GetData;
                        Call.Output();

                        if (GetData.Trim().Length == 0 || !Passed) Owner.RegisterFailure(this); else Owner.RegisterSuccess(this);
                    }
                    else
                    {
                        ShouldRefund = true;
                    }
                }
                else if (this.Type == 1)
                {
                    if (PlatformHub.IsEnable(this.Name))
                    {
                        AICall Call = new AICall();

                        bool Passed = false;
                        int MaxTry = MaxTranslationAttempts;

                        string ApiKey = "";
                        string GetData = null;

                        do
                        {
                            CancelToken.ThrowIfCancellationRequested();

                            ApiKey = NIMEngine.NIM_Engine.KeyData.GetData(this.Name).GetFirstKey();

                            bool State = PlatformHub.CallAINode(
                                this.Name,
                                ApiKey,
                                NIM_Engine.Config,
                                CustomWords,
                                Source,
                                From,
                                To,
                                UseAIMemory,
                                AIMemoryQueryLimit,
                                AIParam,
                                ref UseModel,
                                ref Call,
                                out string TranslateStr);

                            GetData = TranslateStr.Trim();

                            Passed = TranslatorRef.Preprocessor.SecondaryQualityInspection(GetData, CustomWords);

                            if (!Passed && MaxTry > 0)
                            {
                                Delay(CancelToken, NIMEngine.NIM_Engine.Config.ReTryWaitTime);
                                MaxTry--;
                            }
                            else
                            {
                                break;
                            }
                        } while (!Passed);

                        if (GetData.Length == 0) NIMEngine.NIM_Engine.KeyData.GetData(this.Name).ReportError(ApiKey);

                        TransText = GetData;
                        Call.Output();

                        if (GetData.Trim().Length == 0 || !Passed) Owner.RegisterFailure(this); else Owner.RegisterSuccess(this);
                    }
                    else
                    {
                        ShouldRefund = true;
                    }
                }
                else if (this.Type == 2)
                {
                    if (PlatformHub.IsEnable(this.Name))
                    {
                        var CurrentNode = PlatformHub.GetHumanNode(this.Name);

                        AICall Call = new AICall();
                        string GetData = null;
                        bool Passed = false;
                        int MaxTry = MaxTranslationAttempts;

                        do
                        {
                            CancelToken.ThrowIfCancellationRequested();

                            bool State = PlatformHub.CallHumanNode(
                                this.Name,
                                NIM_Engine.Config,
                                CustomWords,
                                Source,
                                From,
                                To,
                                UseAIMemory,
                                AIMemoryQueryLimit,
                                AIParam,
                                ref Call,
                                out string TranslateStr);

                            GetData = TranslateStr.Trim();

                            Passed = TranslatorRef.Preprocessor.SecondaryQualityInspection(GetData, CustomWords);

                            if (!Passed && MaxTry > 0)
                            {
                                Delay(CancelToken, NIMEngine.NIM_Engine.Config.ReTryWaitTime);
                                MaxTry--;
                            }
                            else
                            {
                                break;
                            }
                        } while (!Passed);

                        TransText = GetData;
                        Call.Output();

                        if (GetData.Trim().Length == 0 || !Passed) Owner.RegisterFailure(this); else Owner.RegisterSuccess(this);
                    }
                    else
                    {
                        ShouldRefund = true;
                    }
                }

                return TransText.Trim();
            }
            catch (OperationCanceledException)
            {
                Owner.RefundWeight(this);
                throw;
            }
            catch
            {
                Owner.RegisterFailure(this);
                throw;
            }
            finally
            {
                if (ShouldRefund)
                {
                    Owner.RefundWeight(this);
                }
            }
        }
    }

    internal void RefundWeight(EngineNode Node)
    {
        lock (_Lock)
        {
            if (Node.EffectiveWeight < Node.MaxCallCount) Node.EffectiveWeight++;
        }
    }
}