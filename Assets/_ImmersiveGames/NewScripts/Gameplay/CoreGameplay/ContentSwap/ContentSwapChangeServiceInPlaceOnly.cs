// Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Logging;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.ContentSwap
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class ContentSwapChangeServiceInPlaceOnly : IContentSwapChangeService
    {
        private readonly IContentSwapContextService _contentSwapContext;
        private int _inProgress;

        public ContentSwapChangeServiceInPlaceOnly(IContentSwapContextService contentSwapContext)
        {
            _contentSwapContext = contentSwapContext ?? throw new ArgumentNullException(nameof(contentSwapContext));
        }

        public Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason)
        {
            return RequestContentSwapInPlaceAsync(plan, reason, null);
        }

        public Task RequestContentSwapInPlaceAsync(string contentId, string reason, ContentSwapOptions? options = null)
        {
            return RequestContentSwapInPlaceAsync(BuildPlan(contentId), reason, options);
        }

        public Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason, ContentSwapOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                    "[ContentSwap] Ignorando RequestContentSwapInPlaceAsync com ContentSwapPlan inválido.");
                return Task.CompletedTask;
            }

            string normalizedReason = NormalizeReason(reason);

            DebugUtility.Log<ContentSwapChangeServiceInPlaceOnly>(
                $"[OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode={ContentSwapMode.InPlace} contentId='{plan.ContentId}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                    "[ContentSwap] Já existe uma troca de conteúdo em progresso. Ignorando (InPlace)."
                );
                return Task.CompletedTask;
            }

            var normalizedOptions = NormalizeOptions(options);
            if (normalizedOptions.UseFade || normalizedOptions.UseLoadingHud)
            {
                DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                    "[ContentSwap] ContentSwap é InPlace-only e não integra com SceneFlow; ignorando Fade/LoadingHUD.");
            }

            try
            {
                _contentSwapContext.SetPending(plan, normalizedReason);

                if (!_contentSwapContext.TryCommitPending(normalizedReason, out _))
                {
                    DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                        $"[ContentSwap] TryCommitPending falhou. plan='{plan}' reason='{normalizedReason}'.");
                }
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<ContentSwapChangeServiceInPlaceOnly>(
                    $"[ContentSwap] Falha no InPlace-only. Limpando pending por segurança. ex={ex}");

                _contentSwapContext.ClearPending($"ContentSwap/InPlaceOnly failed: {ex.GetType().Name}");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }

            return Task.CompletedTask;
        }

        private static ContentSwapPlan BuildPlan(string contentId)
        {
            return new ContentSwapPlan(contentId, string.Empty);
        }

        private static ContentSwapOptions NormalizeOptions(ContentSwapOptions? options)
        {
            var normalized = options?.Clone() ?? ContentSwapOptions.Default.Clone();

            if (normalized.TimeoutMs <= 0)
            {
                normalized.TimeoutMs = ContentSwapOptions.DefaultTimeoutMs;
            }

            return normalized;
        }

        private static string NormalizeReason(string reason)
        {
            string sanitized = Sanitize(reason);
            return string.Equals(sanitized, "n/a", StringComparison.Ordinal)
                ? "ContentSwap/InPlaceOnly"
                : sanitized;
        }

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
