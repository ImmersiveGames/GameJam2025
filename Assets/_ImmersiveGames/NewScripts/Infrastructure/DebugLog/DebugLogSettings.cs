using UnityEngine;

namespace _ImmersiveGames.NewScripts.Infrastructure.DebugLog
{
    /// <summary>
    /// Definições persistentes para o DebugUtility, permitindo configurar comportamento em editor ou player.
    /// </summary>
    [CreateAssetMenu(
        menuName = "ImmersiveGames/Debug/DebugLog Settings",
        fileName = "DebugLogSettings")]
    public sealed class DebugLogSettings : ScriptableObject
    {
        [Header("Flags globais")]
        public bool globalDebugEnabled = true;
        public bool verboseInEditor = true;
        public bool verboseInPlayer = false;
        public bool fallbacksInEditor = true;
        public bool fallbacksInPlayer = false;
        public bool repeatedCallVerboseInEditor = true;
        public bool repeatedCallVerboseInPlayer = true;

        [Header("Nível padrão")]
        public DebugLevel editorDefaultLevel = DebugLevel.Logs;
        public DebugLevel playerDefaultLevel = DebugLevel.Warning;

        [Header("Modo de depuração forçada")]
        public bool forceDebugMode;
        public DebugLevel debugModeLevel = DebugLevel.Verbose;
    }
}
