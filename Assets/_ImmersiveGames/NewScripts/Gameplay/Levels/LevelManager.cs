#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.Scene;

namespace _ImmersiveGames.NewScripts.Gameplay.Levels
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class LevelManager : ILevelManager
    {
        private const string LevelChangePrefix = "LevelChange/";
        private const string QaLevelPrefix = "QA/Levels/";

        private readonly IContentSwapChangeService _contentSwapChangeService;
        private int _inProgress;

        public LevelManager(IContentSwapChangeService contentSwapChangeService)
        {
            _contentSwapChangeService = contentSwapChangeService ?? throw new ArgumentNullException(nameof(contentSwapChangeService));
        }

        public async Task RequestLevelInPlaceAsync(LevelPlan plan, string reason, LevelChangeOptions? options = null)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Ignorando RequestLevelInPlaceAsync com LevelPlan inválido.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Já existe mudança de nível em progresso. Ignorando RequestLevelInPlaceAsync.");
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            var normalizedOptions = NormalizeOptions(options);

            DebugUtility.Log<LevelManager>(
                $"[OBS][Level] LevelChangeRequested levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='InPlace' reason='{normalizedReason}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);

            try
            {
                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeStarted levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='InPlace' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                await _contentSwapChangeService.RequestContentSwapInPlaceAsync(
                    plan.ToContentSwapPlan(),
                    normalizedReason,
                    normalizedOptions.ContentSwapOptions);

                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeCompleted levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='InPlace' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[Level] Falha ao mudar nível (InPlace). levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        public async Task RequestLevelWithTransitionAsync(LevelPlan plan, SceneTransitionRequest transition, string reason, LevelChangeOptions? options = null)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Ignorando RequestLevelWithTransitionAsync com LevelPlan inválido.");
                return;
            }

            if (transition == null)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Transition request nulo. RequestLevelWithTransitionAsync ignorado.");
                return;
            }

            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<LevelManager>(
                    "[Level] Já existe mudança de nível em progresso. Ignorando RequestLevelWithTransitionAsync.");
                return;
            }

            var normalizedReason = NormalizeReason(reason);
            var normalizedOptions = NormalizeOptions(options);

            if (!SupportsWithTransition(_contentSwapChangeService, out var rejectionReason))
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[WARN][ContentSwap] ContentSwapRejected mode={ContentSwapMode.SceneTransition} contentId='{plan.ContentId}' reason='{normalizedReason}' rejectionReason='{rejectionReason}'.");

                EventBus<ContentSwapRejectedEvent>.Raise(
                    new ContentSwapRejectedEvent(plan.ToContentSwapPlan(), ContentSwapMode.SceneTransition, normalizedReason, rejectionReason));
                Interlocked.Exchange(ref _inProgress, 0);
                return;
            }

            DebugUtility.Log<LevelManager>(
                $"[OBS][Level] LevelChangeRequested levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='SceneTransition' reason='{normalizedReason}' contentSig='{plan.ContentSignature}'.",
                DebugUtility.Colors.Info);

            try
            {
                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeStarted levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='SceneTransition' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);

                await _contentSwapChangeService.RequestContentSwapWithTransitionAsync(
                    plan.ToContentSwapPlan(),
                    transition,
                    normalizedReason,
                    normalizedOptions.ContentSwapOptions);

                DebugUtility.Log<LevelManager>(
                    $"[OBS][Level] LevelChangeCompleted levelId='{plan.LevelId}' contentId='{plan.ContentId}' mode='SceneTransition' reason='{normalizedReason}'.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogWarning<LevelManager>(
                    $"[Level] Falha ao mudar nível (SceneTransition). levelId='{plan.LevelId}' ex='{ex.GetType().Name}: {ex.Message}'.");
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
        }

        private static string NormalizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "LevelChange/GoTo";
            }

            var trimmed = reason.Trim();
            if (trimmed.StartsWith(LevelChangePrefix, StringComparison.Ordinal)
                || trimmed.StartsWith(QaLevelPrefix, StringComparison.Ordinal))
            {
                return trimmed;
            }

            return $"{LevelChangePrefix}{trimmed}";
        }

        private static LevelChangeOptions NormalizeOptions(LevelChangeOptions? options)
        {
            return options?.Clone() ?? LevelChangeOptions.Default.Clone();
        }

        private static bool SupportsWithTransition(IContentSwapChangeService service, out string rejectionReason)
        {
            var caps = service as IContentSwapChangeServiceCapabilities;
            if (caps == null)
            {
                rejectionReason = "ContentSwap/WithTransitionUnavailable";
                return false;
            }

            if (!caps.SupportsWithTransition)
            {
                rejectionReason = string.IsNullOrWhiteSpace(caps.CapabilityReason)
                    ? "ContentSwap/WithTransitionUnavailable"
                    : caps.CapabilityReason;
                return false;
            }

            rejectionReason = string.Empty;
            return true;
        }
    }
}
