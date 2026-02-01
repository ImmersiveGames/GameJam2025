#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.DebugLog;
using _ImmersiveGames.NewScripts.Gameplay.ContentSwap;

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

            string normalizedReason = NormalizeReason(reason);
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


        private static string NormalizeReason(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return "LevelChange/GoTo";
            }

            string trimmed = reason.Trim();
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

    }
}
