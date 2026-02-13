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
        private static readonly object Sync = new();
        private static bool _isFatalLatched;
        private static bool _ignoredAlreadyLatchedLogged;

        public static bool IsFatalLatched => _isFatalLatched;

        public static void FailFast(string category, string message, UnityEngine.Object context = null)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                category = "Runtime";
            }

            bool shouldLogFatal = false;
            bool shouldLogIgnored = false;

            lock (Sync)
            {
                if (!_isFatalLatched)
                {
                    _isFatalLatched = true;
                    shouldLogFatal = true;
                }
                else if (!_ignoredAlreadyLatchedLogged)
                {
                    _ignoredAlreadyLatchedLogged = true;
                    shouldLogIgnored = true;
                }
            }

            if (shouldLogFatal)
            {
                var fatalMessage = $"[FATAL][{category}] {message} (fatal latched).";

                if (context != null)
                {
                    DebugUtility.LogError<RuntimeFailFastUtility>(fatalMessage, context);
                }
                else
                {
                    DebugUtility.LogError(typeof(RuntimeFailFastUtility), fatalMessage);
                }
            }
            else if (shouldLogIgnored)
            {
                DebugUtility.LogVerbose(typeof(RuntimeFailFastUtility),
                    "[OBS][Boot] FailFast ignored: already fatal latched.",
                    DebugUtility.Colors.Info);
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            UnityEditor.EditorApplication.ExitPlaymode();
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
