using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Core.Logging
{
    public static partial class HardFailFastH1
    {
        public static void Trigger<T>(string detail, Exception innerException = null)
        {
            Trigger(typeof(T), detail, innerException);
        }

        public static void Trigger(Type ownerType, string detail, Exception innerException = null)
        {
            string message = detail ?? string.Empty;
            if (!message.StartsWith("[FATAL][H1]", StringComparison.Ordinal))
            {
                message = $"[FATAL][H1] {message}";
            }

            DebugUtility.LogError(ownerType ?? typeof(HardFailFastH1), message);

            StopPlayModeOrQuit();

            throw innerException == null
                ? new InvalidOperationException(message)
                : new InvalidOperationException(message, innerException);
        }

        private static void StopPlayModeOrQuit()
        {
            RequestEditorStopPlayMode();

            if (!Application.isEditor)
            {
                Application.Quit();
            }
        }

        static partial void RequestEditorStopPlayMode();
    }
}
