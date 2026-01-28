// Assets/_ImmersiveGames/NewScripts/Gameplay/ContentSwap/ContentSwapChangeServiceInPlaceOnly.cs
#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.ContentSwap
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

            DebugUtility.Log<ContentSwapChangeServiceInPlaceOnly>(
                $"[OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode={ContentSwapMode.InPlace} contentId='{plan.ContentId}' reason='{Sanitize(reason)}'",
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
                    "[ContentSwap] InPlace-only ignora Fade/LoadingHUD. Use o fluxo completo quando SceneFlow for reintroduzido.");
            }

            try
            {
                _contentSwapContext.SetPending(plan, reason);

                if (!_contentSwapContext.TryCommitPending(reason ?? "ContentSwap/InPlaceOnly", out _))
                {
                    DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                        $"[ContentSwap] TryCommitPending falhou. plan='{plan}' reason='{Sanitize(reason)}'.");
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

        public Task RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason)
        {
            return RequestContentSwapWithTransitionAsync(plan, transition, reason, null);
        }

        public Task RequestContentSwapWithTransitionAsync(string contentId, SceneTransitionRequest transition, string reason, ContentSwapOptions? options = null)
        {
            return RequestContentSwapWithTransitionAsync(BuildPlan(contentId), transition, reason, options);
        }

        public Task RequestContentSwapWithTransitionAsync(ContentSwapPlan plan, SceneTransitionRequest transition, string reason, ContentSwapOptions? options)
        {
            DebugUtility.LogWarning<ContentSwapChangeServiceInPlaceOnly>(
                "[ContentSwap] WithTransition indisponível no modo ContentSwap-only. Executando commit InPlace imediato.");

            return RequestContentSwapInPlaceAsync(plan, reason, options);
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

        private static string Sanitize(string? s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
