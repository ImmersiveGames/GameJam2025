using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.NewScripts.Modules.SceneFlow.Fade.Runtime
{
    /// <summary>
    /// Serviço de fade degradado: transições seguem sem efeito visual de fade.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class DegradedFadeService : IFadeService
    {
        private readonly string _reason;

        public DegradedFadeService(string reason)
        {
            _reason = string.IsNullOrWhiteSpace(reason) ? "unspecified" : reason.Trim();
        }

        public Task EnsureReadyAsync()
        {
            return Task.CompletedTask;
        }

        public void Configure(FadeConfig config)
        {
            // Comentário: em degraded, configuração de fade é ignorada.
        }

        public Task FadeInAsync(string? contextSignature = null)
        {
            DebugUtility.LogVerbose<DegradedFadeService>(
                $"[DEGRADED][Fade] FadeIn skipped. reason='{_reason}', signature='{contextSignature ?? "n/a"}'.");
            return Task.CompletedTask;
        }

        public Task FadeOutAsync(string? contextSignature = null)
        {
            DebugUtility.LogVerbose<DegradedFadeService>(
                $"[DEGRADED][Fade] FadeOut skipped. reason='{_reason}', signature='{contextSignature ?? "n/a"}'.");
            return Task.CompletedTask;
        }
    }
}
