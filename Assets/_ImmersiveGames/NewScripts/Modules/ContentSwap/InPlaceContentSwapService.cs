#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Core.Composition;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Infrastructure.RuntimeMode;
using _ImmersiveGames.NewScripts.Modules.Gates;
namespace _ImmersiveGames.NewScripts.Modules.ContentSwap
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class InPlaceContentSwapService : IContentSwapChangeService
    {
        private const string DegradedFeature = "content_swap";
        private const int GatePollIntervalMs = 50;

        private readonly IContentSwapContextService _contentSwapContext;
        private int _inProgress;

        private bool _dependenciesResolved;
        private ISimulationGateService? _gateService;
        private IRuntimeModeProvider? _runtimeModeProvider;
        private IDegradedModeReporter? _degradedModeReporter;

        public InPlaceContentSwapService(IContentSwapContextService contentSwapContext)
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

        public async Task RequestContentSwapInPlaceAsync(ContentSwapPlan plan, string reason, ContentSwapOptions? options)
        {
            if (!plan.IsValid)
            {
                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    "[ContentSwap] Ignorando RequestContentSwapInPlaceAsync com ContentSwapPlan inv\u00e1lido.");
                return;
            }

            string normalizedReason = NormalizeReason(reason);

            LogRequest(plan, normalizedReason);

            if (!TryEnterInProgress())
            {
                return;
            }

            try
            {
                EnsureDependencies();

                var normalizedOptions = NormalizeOptions(options);
                WarnUnsupportedOptions(normalizedOptions);

                if (!await EnsureGatesOpenAsync(plan, normalizedReason, normalizedOptions))
                {
                    return;
                }

                CommitSwap(plan, normalizedReason);
            }
            catch (Exception ex)
            {
                HandleFailure(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _inProgress, 0);
            }
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

        private static void LogRequest(ContentSwapPlan plan, string normalizedReason)
        {
            DebugUtility.Log<InPlaceContentSwapService>(
                $"[OBS][ContentSwap] ContentSwapRequested event=content_swap_inplace mode={ContentSwapMode.InPlace} contentId='{plan.contentId}' reason='{normalizedReason}'",
                DebugUtility.Colors.Info);
        }

        private bool TryEnterInProgress()
        {
            if (Interlocked.CompareExchange(ref _inProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    "[ContentSwap] J\u00e1 existe uma troca de conte\u00fado em progresso. Ignorando (InPlace)."
                );
                return false;
            }

            return true;
        }

        private static void WarnUnsupportedOptions(ContentSwapOptions options)
        {
            if (options.UseFade || options.UseLoadingHud)
            {
                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    "[ContentSwap] ContentSwap \u00e9 InPlace-only e n\u00e3o integra com SceneFlow; ignorando Fade/LoadingHUD.");
            }
        }

        private void CommitSwap(ContentSwapPlan plan, string normalizedReason)
        {
            _contentSwapContext.SetPending(plan, normalizedReason);

            if (!_contentSwapContext.TryCommitPending(normalizedReason, out _))
            {
                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    $"[ContentSwap] TryCommitPending falhou. plan='{plan}' reason='{normalizedReason}'.");
            }
        }

        private void HandleFailure(Exception ex)
        {
            DebugUtility.LogError<InPlaceContentSwapService>(
                $"[ContentSwap] Falha no InPlace-only. Limpando pending por seguran\u00e7a. ex={ex}");

            _contentSwapContext.ClearPending($"ContentSwap/InPlaceOnly failed: {ex.GetType().Name}");
        }

        private void EnsureDependencies()
        {
            if (_dependenciesResolved)
            {
                return;
            }

            if (DependencyManager.HasInstance)
            {
                var provider = DependencyManager.Provider;
                provider.TryGetGlobal(out _gateService);
                provider.TryGetGlobal(out _runtimeModeProvider);
                provider.TryGetGlobal(out _degradedModeReporter);
            }

            _runtimeModeProvider ??= new UnityRuntimeModeProvider();
            _degradedModeReporter ??= new DegradedModeReporter();

            _dependenciesResolved = true;
        }

        private async Task<bool> EnsureGatesOpenAsync(ContentSwapPlan plan, string reason, ContentSwapOptions options)
        {
            bool isStrict = _runtimeModeProvider is { IsStrict: true };

            if (_gateService == null)
            {
                const string detail = "ISimulationGateService ausente (DI global).";

                if (isStrict)
                {
                    DebugUtility.LogError<InPlaceContentSwapService>(
                        $"[ContentSwap] Gate service ausente em Strict. contentId='{plan.contentId}' reason='{reason}'.");
                    throw new InvalidOperationException($"[ContentSwap] Gate service ausente em Strict. {detail}");
                }

                _degradedModeReporter?.Report(
                    feature: DegradedFeature,
                    reason: "missing_gate_service",
                    detail: detail);

                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    $"[ContentSwap] Gate service ausente. Abortando InPlace. contentId='{plan.contentId}' reason='{reason}'.");
                return false;
            }

            if (!TryGetBlockedTokens(out string blockedTokens))
            {
                return true;
            }

            if (isStrict)
            {
                DebugUtility.LogError<InPlaceContentSwapService>(
                    $"[ContentSwap] Gate fechado em Strict. tokens='{blockedTokens}' contentId='{plan.contentId}' reason='{reason}'.");
                throw new InvalidOperationException(
                    $"[ContentSwap] Gate fechado em Strict. tokens='{blockedTokens}' contentId='{plan.contentId}' reason='{reason}'.");
            }

            int timeoutMs = options.TimeoutMs > 0 ? options.TimeoutMs : ContentSwapOptions.DefaultTimeoutMs;

            DebugUtility.LogWarning<InPlaceContentSwapService>(
                $"[ContentSwap] Gate fechado. Aguardando abertura (timeoutMs={timeoutMs}) tokens='{blockedTokens}' contentId='{plan.contentId}' reason='{reason}'.");

            bool opened = await WaitForGatesToOpenAsync(timeoutMs);
            if (!opened)
            {
                string detail = $"tokens='{blockedTokens}' timeoutMs={timeoutMs} contentId='{plan.contentId}' reason='{reason}'";

                _degradedModeReporter?.Report(
                    feature: DegradedFeature,
                    reason: "gate_timeout",
                    detail: detail);

                DebugUtility.LogWarning<InPlaceContentSwapService>(
                    $"[ContentSwap] Gate permaneceu fechado. Abortando InPlace. {detail}");
                return false;
            }

            DebugUtility.LogVerbose<InPlaceContentSwapService>(
                $"[ContentSwap] Gate liberado. Prosseguindo InPlace. contentId='{plan.contentId}' reason='{reason}'.");
            return true;
        }

        private bool AreBlockedTokensActive()
        {
            return TryGetBlockedTokens(out _);
        }

        private async Task<bool> WaitForGatesToOpenAsync(int timeoutMs)
        {
            int start = Environment.TickCount;

            while (AreBlockedTokensActive())
            {
                int elapsed = unchecked(Environment.TickCount - start);
                if (elapsed >= timeoutMs)
                {
                    return false;
                }

                await Task.Delay(GatePollIntervalMs);
            }

            return true;
        }

        private bool TryGetBlockedTokens(out string blockedTokens)
        {
            blockedTokens = "none";
            if (_gateService == null)
            {
                return false;
            }

            bool sceneTransition = _gateService.IsTokenActive(SimulationGateTokens.SceneTransition);
            bool gameplay = _gateService.IsTokenActive(SimulationGateTokens.GameplaySimulation);

            if (!sceneTransition && !gameplay)
            {
                return false;
            }

            if (sceneTransition && gameplay)
            {
                blockedTokens = $"{SimulationGateTokens.SceneTransition},{SimulationGateTokens.GameplaySimulation}";
                return true;
            }

            if (sceneTransition)
            {
                blockedTokens = SimulationGateTokens.SceneTransition;
                return true;
            }

            blockedTokens = SimulationGateTokens.GameplaySimulation;
            return true;
        }

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }
}
