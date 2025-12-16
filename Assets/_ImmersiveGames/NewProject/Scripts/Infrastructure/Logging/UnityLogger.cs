using System;
using UnityEngine;

namespace _ImmersiveGames.NewProject.Infrastructure.Logging
{
    /// <summary>
    /// Logger simples usando o console do Unity. Evita acoplamento de gameplay.
    /// </summary>
    public sealed class UnityLogger : ILogger
    {
        private readonly string _category;
        private readonly LogLevel _minimumLevel;

        public UnityLogger(string category, LogLevel minimumLevel = LogLevel.Info)
        {
            _category = category;
            _minimumLevel = minimumLevel;
        }

        public void Log(LogLevel level, string message, Exception exception = null)
        {
            if (level < _minimumLevel)
            {
                return;
            }

            var formatted = $"[{_category}][{level}] {message}";

            // Comentário em português: delega para o Debug conforme o nível.
            switch (level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                case LogLevel.Error:
                case LogLevel.Critical:
                    Debug.LogError(exception != null ? $"{formatted} :: {exception}" : formatted);
                    break;
                default:
                    Debug.Log(exception != null ? $"{formatted} :: {exception}" : formatted);
                    break;
            }
        }
    }
}
