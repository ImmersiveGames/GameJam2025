using System;

namespace _ImmersiveGames.NewProject.Infrastructure.Logging
{
    /// <summary>
    /// Contrato básico de logger para a infraestrutura global.
    /// Comentários em português para facilitar o onboarding da equipe local.
    /// </summary>
    public interface ILogger
    {
        void Log(LogLevel level, string message, Exception exception = null);
    }
}
