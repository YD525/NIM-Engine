using System;
using System.Collections.Generic;
using System.Threading;
using NIMEngine.Language;

namespace NIMEngine.Additional
{
    public static class SpeechHelper
    {
        private static readonly object VoiceLock = new object();
        private static dynamic VoiceInstance = null;

        private static readonly Dictionary<string, string[]> VoiceHints = new Dictionary<string, string[]>()
        {
        { "English", new[] { "English", "David", "Zira", "George" } },
        { "SimplifiedChinese", new[] { "Chinese", "Huihui", "Zh-cn" } },
        { "TraditionalChinese", new[] { "Chinese (Traditional)", "Zh-hk", "Zh-tw" } },
        { "Japanese", new[] { "Japanese", "Haruka", "Ja-jp" } },
        { "German", new[] { "German", "De-de" } },
        { "Korean", new[] { "Korean", "Heami", "Ko-kr" } },
        { "Turkish", new[] { "Turkish", "Tr-tr" } },
        { "Brazilian", new[] { "Portuguese", "Pt-br" } },
        { "Russian", new[] { "Russian", "Ru-ru" } },
        { "Italian", new[] { "Italian", "It-it" } },
        { "Spanish", new[] { "Spanish", "Es-es" } },
        { "Hindi", new[] { "Hindi", "Hi-in" } },
        { "Urdu", new[] { "Urdu", "Ur-pk" } },
        { "Indonesian", new[] { "Indonesian", "Id-id" } }
        };

        public static void TryPlaySound(P_Language To,string Text,bool CanCreatTrd = false)
        {
            Action PlaySoundAction = new Action(() => {
                try
                {
                    P_Language DetectLang = P_Language.Detect(Text,LanguageDetectMode.WholeText);

                    if (DetectLang.Name == "Japanese" ||
                        DetectLang.Name == "SimplifiedChinese" ||
                        DetectLang.Name == "TraditionalChinese")
                    {
                        if (To.Name == "Japanese" ||
                            To.Name == "SimplifiedChinese" ||
                            To.Name == "TraditionalChinese")
                        {
                            DetectLang = To;
                        }
                    }

                    lock (VoiceLock)
                    {
                        if (VoiceInstance == null)
                        {
                            Type VoiceType = Type.GetTypeFromProgID("SAPI.SpVoice");
                            VoiceInstance = Activator.CreateInstance(VoiceType);
                            VoiceInstance.Volume = 100;
                            VoiceInstance.Rate = 0;
                        }

                        dynamic Voices = VoiceInstance.GetVoices();
                        dynamic BestMatch = null;

                        if (VoiceHints.TryGetValue(DetectLang.Name, out var Hints))
                        {
                            foreach (dynamic Token in Voices)
                            {
                                string Desc = Token.GetDescription().ToString();
                                string LangAttr = Token.GetAttribute("Language")?.ToString() ?? "";

                                foreach (var Hint in Hints)
                                {
                                    if (Desc.IndexOf(Hint, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    LangAttr.IndexOf(Hint, StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        BestMatch = Token;
                                        break;
                                    }
                                }

                                if (BestMatch != null)
                                    break;
                            }
                        }

                        if (BestMatch != null)
                            VoiceInstance.Voice = BestMatch;

                        VoiceInstance.Speak("", 2); // Purge before speak
                        VoiceInstance.Speak(Text, 1); // Async speak
                    }
                }
                catch
                {
                }
            });

            if (!CanCreatTrd)
            {
                PlaySoundAction.Invoke();
            }
            else
            {
                new Thread(() => {
                    PlaySoundAction.Invoke();
                }).Start();
            }
        }
    }
}
