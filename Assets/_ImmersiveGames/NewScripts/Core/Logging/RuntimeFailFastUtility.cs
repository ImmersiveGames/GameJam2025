using System;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Core.Logging
{
    /// <summary>
    /// Utilitário central para encerramento fail-fast em runtime.
    /// Não depende de objetos/cenas para funcionar.
    /// </summary>
    public static class RuntimeFailFastUtility
    {
        public static void FailFast(string category, string message, UnityEngine.Object context = null)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "Runtime";
            }

            var fatalMessage = $"[FATAL][{category}] {message}";

            if (context != null)
            {
                DebugUtility.LogError<RuntimeFailFastUtility>(fatalMessage, context);
            }
            else
            {
                DebugUtility.LogError(typeof(RuntimeFailFastUtility), fatalMessage);
            }

#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorApplication.ExitPlaymode();
                UnityEditor.EditorApplication.isPlaying = false;
            }
#else
            Application.Quit(1);
#endif
        }

        public static InvalidOperationException FailFastAndCreateException(
            string category,
            string message,
            UnityEngine.Object context = null)
        {
            FailFast(category, message, context);
            return new InvalidOperationException($"[FATAL][{category}] {message}");
        }
    }
}
