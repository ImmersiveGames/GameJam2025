#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using _ImmersiveGames.NewScripts.Infrastructure.SceneFlow;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Descreve um pedido explícito de transição de cena para o pipeline NewScripts.
    /// </summary>
    public sealed class SceneTransitionRequest
    {
        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }

        public SceneFlowProfileId TransitionProfileId { get; }

        // Compatibilidade: logging / debug pode exibir o texto do profile.
        public string TransitionProfileName => TransitionProfileId.Value;

        /// <summary>
        /// Assinatura/correlation id para observabilidade do contexto.
        /// Em geral é preenchida por SceneTransitionSignatureUtil.Compute(BuildContext(request)).
        /// </summary>
        public string ContextSignature { get; }

        /// <summary>
        /// (Opcional) Origem do pedido para diagnóstico (ex.: "QA/ContentSwap/WithTransition/G03", "Navigation/MenuPlayButton").
        /// </summary>
        public string RequestedBy { get; }

        public SceneTransitionRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade = true,
            SceneFlowProfileId transitionProfileId = default,
            string? contextSignature = null,
            string? requestedBy = null)
        {
            ScenesToLoad = scenesToLoad;
            ScenesToUnload = scenesToUnload;
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            TransitionProfileId = transitionProfileId;

            // Mantém propriedades não-nulas (evita NRT warnings/erros).
            ContextSignature = string.IsNullOrWhiteSpace(contextSignature) ? string.Empty : contextSignature.Trim();
            RequestedBy = string.IsNullOrWhiteSpace(requestedBy) ? string.Empty : requestedBy.Trim();
        }
    }

    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa a transição completa de acordo com o pedido.
        /// </summary>
        Task TransitionAsync(SceneTransitionRequest request);
    }

    /// <summary>
    /// Adapter para operações de loading/unloading/ActiveScene independente da fonte (SceneManager ou legado).
    /// </summary>
    public interface ISceneFlowLoaderAdapter
    {
        Task LoadSceneAsync(string sceneName);
        Task UnloadSceneAsync(string sceneName);
        bool IsSceneLoaded(string sceneName);
        Task<bool> TrySetActiveSceneAsync(string sceneName);
        string GetActiveSceneName();
    }

    /// <summary>
    /// Adapter para operações de fade desacopladas do legado.
    /// </summary>
    public interface ISceneFlowFadeAdapter
    {
        bool IsAvailable { get; }
        void ConfigureFromProfile(SceneFlowProfileId profileId);
        Task FadeInAsync();
        Task FadeOutAsync();
    }

    /// <summary>
    /// Gate opcional para "segurar" o final da transição (FadeOut/Completed) até que
    /// tarefas externas associadas ao mesmo context (ex: WorldLifecycle reset) concluam.
    /// </summary>
    public interface ISceneTransitionCompletionGate
    {
        Task AwaitBeforeFadeOutAsync(SceneTransitionContext context);
    }

    public sealed class NoOpSceneTransitionCompletionGate : ISceneTransitionCompletionGate
    {
        public Task AwaitBeforeFadeOutAsync(SceneTransitionContext context) => Task.CompletedTask;
    }

    /// <summary>
    /// Implementação nativa do pipeline de Scene Flow emitindo eventos do NewScripts.
    /// </summary>
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        // Dedupe: bloqueia reentrância por assinatura idêntica numa janela curta.
        // Ajuste conforme necessário. 750ms tem sido suficiente para capturar "double start" acidental.
        private const int DuplicateSignatureWindowMs = 750;

        private readonly ISceneFlowLoaderAdapter _loaderAdapter;
        private readonly ISceneFlowFadeAdapter _fadeAdapter;
        private readonly ISceneTransitionCompletionGate _completionGate;

        private readonly SemaphoreSlim _transitionGate = new(1, 1);
        private int _transitionInProgress;

        private long _transitionIdSeq;

        private string _lastStartedSignature = string.Empty;
        private int _lastStartedTick;

        private string _lastCompletedSignature = string.Empty;
        private int _lastCompletedTick;

        public SceneTransitionService(
            ISceneFlowLoaderAdapter loaderAdapter,
            ISceneFlowFadeAdapter fadeAdapter,
            ISceneTransitionCompletionGate? completionGate = null)
        {
            _loaderAdapter = loaderAdapter ?? new SceneManagerLoaderAdapter();
            _fadeAdapter = fadeAdapter ?? new NullFadeAdapter();
            _completionGate = completionGate ?? new NoOpSceneTransitionCompletionGate();
        }

        public async Task TransitionAsync(SceneTransitionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var context = SceneTransitionSignatureUtil.BuildContext(request);
            string signature = SceneTransitionSignatureUtil.Compute(context);

            // Dedupe por assinatura: evita "double start" no mesmo contexto em janela curta.
            // Isto não substitui correção do caller, mas impede o pior: reentrância/interleaving.
            if (ShouldDedupe(signature))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Dedupe: TransitionAsync ignorado (signature repetida em janela curta). " +
                    $"signature='{signature}', requestedBy='{Sanitize(request.RequestedBy)}'.");
                return;
            }

            // Pre-checagem concorrente (rápida).
            if (Interlocked.CompareExchange(ref _transitionInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transição já está em andamento (_transitionInProgress ativo). Ignorando solicitação concorrente. " +
                    $"signature='{signature}', requestedBy='{Sanitize(request.RequestedBy)}'.");
                return;
            }

            // Garante exclusão mútua (não bloqueante).
            if (!_transitionGate.Wait(0))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Uma transição já está em andamento. Ignorando solicitação concorrente. " +
                    $"signature='{signature}', requestedBy='{Sanitize(request.RequestedBy)}'.");
                Interlocked.Exchange(ref _transitionInProgress, 0);
                return;
            }

            var transitionId = Interlocked.Increment(ref _transitionIdSeq);

            try
            {
                MarkStarted(signature);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionStarted id={transitionId} signature='{signature}' profile='{context.TransitionProfileName}' requestedBy='{Sanitize(request.RequestedBy)}' {context}",
                    DebugUtility.Colors.Info);

                EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));

                // FadeIn é a primeira etapa visual. O HUD de Loading pode ser "ensured" em paralelo,
                // mas deve aparecer apenas após o FadeIn estar concluído (Opção A+).
                await RunFadeInIfNeeded(context);

                if (context.UseFade)
                {
                    EventBus<SceneTransitionFadeInCompletedEvent>.Raise(new SceneTransitionFadeInCompletedEvent(context));
                }

                await RunSceneOperationsAsync(context);

                // 1) Momento em que "cenas estão prontas" (load/unload/active done).
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] ScenesReady id={transitionId} signature='{signature}' profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Info);

                // 2) Aguarda gates externos (ex: WorldLifecycle reset) ANTES de revelar (FadeOut).
                await AwaitCompletionGateAsync(context);

                EventBus<SceneTransitionBeforeFadeOutEvent>.Raise(new SceneTransitionBeforeFadeOutEvent(context));

                await RunFadeOutIfNeeded(context);

                EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(context));

                MarkCompleted(signature);

                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] TransitionCompleted id={transitionId} signature='{signature}' profile='{context.TransitionProfileName}'.",
                    DebugUtility.Colors.Success);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SceneTransitionService>(
                    $"[SceneFlow] Erro durante transição: {ex}");
                throw;
            }
            finally
            {
                Interlocked.Exchange(ref _transitionInProgress, 0);
                _transitionGate.Release();
            }
        }

        private bool ShouldDedupe(string signature)
        {
            if (string.IsNullOrWhiteSpace(signature))
            {
                return false;
            }

            var now = Environment.TickCount;

            // Caso 1: repetição imediatamente após Start anterior (start-start).
            if (string.Equals(signature, _lastStartedSignature, StringComparison.Ordinal))
            {
                var dt = unchecked(now - _lastStartedTick);
                if (dt >= 0 && dt <= DuplicateSignatureWindowMs)
                {
                    return true;
                }
            }

            // Caso 2: repetição logo após Completed (completed-start).
            if (string.Equals(signature, _lastCompletedSignature, StringComparison.Ordinal))
            {
                var dt = unchecked(now - _lastCompletedTick);
                if (dt >= 0 && dt <= DuplicateSignatureWindowMs)
                {
                    return true;
                }
            }

            return false;
        }

        private void MarkStarted(string signature)
        {
            _lastStartedSignature = signature ?? string.Empty;
            _lastStartedTick = Environment.TickCount;
        }

        private void MarkCompleted(string signature)
        {
            _lastCompletedSignature = signature ?? string.Empty;
            _lastCompletedTick = Environment.TickCount;
        }

        private async Task AwaitCompletionGateAsync(SceneTransitionContext context)
        {
            try
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Aguardando completion gate antes do FadeOut. signature='{SceneTransitionSignatureUtil.Compute(context)}'.");

                await _completionGate.AwaitBeforeFadeOutAsync(context);

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='{SceneTransitionSignatureUtil.Compute(context)}'.");
            }
            catch (Exception ex)
            {
                // Gate é "best effort": não deve travar a transição.
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Completion gate falhou/abortou. Prosseguindo com FadeOut. ex={ex.GetType().Name}: {ex.Message}");
            }
        }

        private async Task RunFadeInIfNeeded(SceneTransitionContext context)
        {
            if (!context.UseFade)
            {
                return;
            }

            if (!_fadeAdapter.IsAvailable)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    "[SceneFlow] Fade solicitado, porém nenhum adapter está disponível. Continuando sem fade.");
                return;
            }

            _fadeAdapter.ConfigureFromProfile(context.TransitionProfileId);
            await _fadeAdapter.FadeInAsync();
        }

        private async Task RunFadeOutIfNeeded(SceneTransitionContext context)
        {
            if (!context.UseFade)
            {
                return;
            }

            if (!_fadeAdapter.IsAvailable)
            {
                return;
            }

            await _fadeAdapter.FadeOutAsync();
        }

        private async Task RunSceneOperationsAsync(SceneTransitionContext context)
        {
            IReadOnlyList<string> reloadScenes = GetReloadScenes(context.ScenesToLoad, context.ScenesToUnload);
            IReadOnlyList<string> loadScenes = FilterScenesExcluding(context.ScenesToLoad, reloadScenes);
            await LoadScenesAsync(loadScenes);

            if (reloadScenes.Count > 0)
            {
                await HandleReloadScenesAsync(context, reloadScenes);
            }

            await SetActiveSceneAsync(context.TargetActiveScene);

            IReadOnlyList<string> unloadScenes = FilterScenesExcluding(context.ScenesToUnload, reloadScenes);
            await UnloadScenesAsync(unloadScenes, context.TargetActiveScene);
        }

        private async Task LoadScenesAsync(IReadOnlyList<string> scenesToLoad, bool forceLoad = false)
        {
            if (scenesToLoad == null || scenesToLoad.Count == 0)
            {
                return;
            }

            foreach (string sceneName in scenesToLoad)
            {
                if (!forceLoad && _loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Cena '{sceneName}' já está carregada. Pulando load.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Carregando cena '{sceneName}' (Additive)...");

                await _loaderAdapter.LoadSceneAsync(sceneName);
            }
        }

        private async Task SetActiveSceneAsync(string targetActiveScene)
        {
            if (string.IsNullOrWhiteSpace(targetActiveScene))
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[SceneFlow] TargetActiveScene vazio. Mantendo cena ativa atual.");
                return;
            }

            bool success = await _loaderAdapter.TrySetActiveSceneAsync(targetActiveScene);
            if (!success)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Não foi possível definir a cena ativa para '{targetActiveScene}'. Cena atual='{_loaderAdapter.GetActiveSceneName()}'.");
            }
            else
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Cena ativa definida para '{targetActiveScene}'.");
            }
        }

        private async Task UnloadScenesAsync(IReadOnlyList<string> scenesToUnload, string targetActiveScene)
        {
            if (scenesToUnload == null || scenesToUnload.Count == 0)
            {
                return;
            }

            foreach (string sceneName in scenesToUnload)
            {
                if (string.Equals(sceneName, targetActiveScene, StringComparison.Ordinal))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Ignorando unload da cena alvo ativa '{sceneName}'.");
                    continue;
                }

                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Cena '{sceneName}' já está descarregada. Pulando unload.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Descarregando cena '{sceneName}'...");

                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private async Task HandleReloadScenesAsync(SceneTransitionContext context, IReadOnlyList<string> reloadScenes)
        {
            string tempActive = ResolveTempActiveScene(reloadScenes);
            if (!string.IsNullOrWhiteSpace(tempActive) &&
                !string.Equals(tempActive, _loaderAdapter.GetActiveSceneName(), StringComparison.Ordinal))
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Reload: definindo cena ativa temporária '{tempActive}'.");
                await SetActiveSceneAsync(tempActive);
            }

            await UnloadScenesForReloadAsync(reloadScenes);
            await LoadScenesAsync(reloadScenes, forceLoad: true);

            DebugUtility.LogVerbose<SceneTransitionService>(
                $"[SceneFlow] Reload concluído. TargetActiveScene='{context.TargetActiveScene}'.");
        }

        private async Task UnloadScenesForReloadAsync(IReadOnlyList<string> reloadScenes)
        {
            foreach (string sceneName in reloadScenes)
            {
                if (!_loaderAdapter.IsSceneLoaded(sceneName))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[SceneFlow] Reload: cena '{sceneName}' já está descarregada. Pulando unload.");
                    continue;
                }

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Reload: descarregando cena '{sceneName}'...");

                await _loaderAdapter.UnloadSceneAsync(sceneName);
            }
        }

        private static IReadOnlyList<string> GetReloadScenes(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload)
        {
            if (scenesToLoad == null || scenesToUnload == null)
            {
                return Array.Empty<string>();
            }

            var unloadSet = new HashSet<string>(scenesToUnload, StringComparer.Ordinal);
            var reload = new List<string>();

            foreach (string sceneName in scenesToLoad)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    continue;
                }

                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (unloadSet.Contains(trimmed))
                {
                    reload.Add(trimmed);
                }
            }

            return reload;
        }

        private static IReadOnlyList<string> FilterScenesExcluding(
            IReadOnlyList<string> scenes,
            IReadOnlyList<string> excludedScenes)
        {
            if (scenes == null || scenes.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (excludedScenes == null || excludedScenes.Count == 0)
            {
                return scenes;
            }

            var excludeSet = new HashSet<string>(excludedScenes, StringComparer.Ordinal);
            var filtered = new List<string>();

            foreach (string sceneName in scenes)
            {
                if (string.IsNullOrWhiteSpace(sceneName))
                {
                    continue;
                }

                string trimmed = sceneName.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                if (!excludeSet.Contains(trimmed))
                {
                    filtered.Add(trimmed);
                }
            }

            return filtered;
        }

        private string ResolveTempActiveScene(IReadOnlyList<string> reloadScenes)
        {
            var reloadSet = new HashSet<string>(reloadScenes, StringComparer.Ordinal);
            const string UiGlobalSceneName = "UIGlobalScene";

            if (_loaderAdapter.IsSceneLoaded(UiGlobalSceneName) && !reloadSet.Contains(UiGlobalSceneName))
            {
                return UiGlobalSceneName;
            }

            string currentActive = _loaderAdapter.GetActiveSceneName();
            if (!string.IsNullOrWhiteSpace(currentActive) && !reloadSet.Contains(currentActive))
            {
                return currentActive;
            }

            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                string name = scene.name;
                if (!string.IsNullOrWhiteSpace(name) && !reloadSet.Contains(name))
                {
                    return name;
                }
            }

            return string.Empty;
        }

        private static string Sanitize(string s)
            => string.IsNullOrWhiteSpace(s) ? "n/a" : s.Replace("\n", " ").Replace("\r", " ").Trim();
    }

    /// <summary>
    /// Implementação padrão de loader usando SceneManager (fallback).
    /// </summary>
    public sealed class SceneManagerLoaderAdapter : ISceneFlowLoaderAdapter
    {
        public async Task LoadSceneAsync(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] LoadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        public async Task UnloadSceneAsync(string sceneName)
        {
            var op = SceneManager.UnloadSceneAsync(sceneName);
            if (op == null)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] UnloadSceneAsync retornou null para '{sceneName}'.");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        public async Task<bool> TrySetActiveSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] Cena '{sceneName}' inválida para SetActiveScene.");
                return false;
            }

            if (!scene.isLoaded)
            {
                DebugUtility.LogWarning<SceneManagerLoaderAdapter>(
                    $"[SceneFlow] Cena '{sceneName}' não está carregada para SetActiveScene.");
                return false;
            }

            SceneManager.SetActiveScene(scene);
            await Task.Yield();
            return true;
        }

        public string GetActiveSceneName()
        {
            return SceneManager.GetActiveScene().name;
        }
    }

    /// <summary>
    /// Adapter nulo para cenários sem fade configurado.
    /// </summary>
    public sealed class NullFadeAdapter : ISceneFlowFadeAdapter
    {
        public bool IsAvailable => false;

        public void ConfigureFromProfile(SceneFlowProfileId profileId) { }

        public Task FadeInAsync() => Task.CompletedTask;

        public Task FadeOutAsync() => Task.CompletedTask;
    }
}
