using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;
using _ImmersiveGames.NewScripts.Infrastructure.Events;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.NewScripts.Infrastructure.Scene
{
    /// <summary>
    /// Descreve um pedido explícito de transição de cena para o pipeline NewScripts.
    /// </summary>
    public sealed class SceneTransitionRequest
    {
        public SceneTransitionRequest(
            IReadOnlyList<string> scenesToLoad,
            IReadOnlyList<string> scenesToUnload,
            string targetActiveScene,
            bool useFade,
            string transitionProfileName = null)
        {
            ScenesToLoad = scenesToLoad ?? Array.Empty<string>();
            ScenesToUnload = scenesToUnload ?? Array.Empty<string>();
            TargetActiveScene = targetActiveScene;
            UseFade = useFade;
            TransitionProfileName = transitionProfileName;
        }

        public IReadOnlyList<string> ScenesToLoad { get; }
        public IReadOnlyList<string> ScenesToUnload { get; }
        public string TargetActiveScene { get; }
        public bool UseFade { get; }
        public string TransitionProfileName { get; }
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
        void ConfigureFromProfile(string profileName);
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
        private readonly ISceneFlowLoaderAdapter _loaderAdapter;
        private readonly ISceneFlowFadeAdapter _fadeAdapter;
        private readonly ISceneTransitionCompletionGate _completionGate;

        private readonly SemaphoreSlim _transitionGate = new(1, 1);
        private int _transitionInProgress;

        public SceneTransitionService(
            ISceneFlowLoaderAdapter loaderAdapter,
            ISceneFlowFadeAdapter fadeAdapter,
            ISceneTransitionCompletionGate completionGate = null)
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

            if (Interlocked.CompareExchange(ref _transitionInProgress, 1, 0) == 1)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    "[SceneFlow] Uma transição já está em andamento (_transitionInProgress ativo). Ignorando solicitação concorrente.");
                return;
            }

            if (!_transitionGate.Wait(0))
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    "[SceneFlow] Uma transição já está em andamento. Ignorando solicitação concorrente.");
                Interlocked.Exchange(ref _transitionInProgress, 0);
                return;
            }

            var context = BuildContext(request);

            try
            {
                DebugUtility.Log<SceneTransitionService>(
                    $"[SceneFlow] Iniciando transição: {context}",
                    DebugUtility.Colors.Info);

                EventBus<SceneTransitionStartedEvent>.Raise(new SceneTransitionStartedEvent(context));

                await RunFadeInIfNeeded(context);

                await RunSceneOperationsAsync(context);

                // 1) Momento em que "cenas estão prontas" (load/unload/active done).
                EventBus<SceneTransitionScenesReadyEvent>.Raise(new SceneTransitionScenesReadyEvent(context));

                // 2) Novo: aguarda gates externos (ex: WorldLifecycle reset) ANTES de revelar (FadeOut).
                await AwaitCompletionGateAsync(context);

                EventBus<SceneTransitionBeforeFadeOutEvent>.Raise(new SceneTransitionBeforeFadeOutEvent(context));

                await RunFadeOutIfNeeded(context);

                EventBus<SceneTransitionCompletedEvent>.Raise(new SceneTransitionCompletedEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    "[SceneFlow] Transição concluída com sucesso.",
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

        private async Task AwaitCompletionGateAsync(SceneTransitionContext context)
        {
            try
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Aguardando completion gate antes do FadeOut. signature='{context}'.");

                await _completionGate.AwaitBeforeFadeOutAsync(context);

                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[SceneFlow] Completion gate concluído. Prosseguindo para FadeOut. signature='{context}'.");
            }
            catch (Exception ex)
            {
                // Gate é "best effort": não deve travar a transição.
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"[SceneFlow] Completion gate falhou/abortou. Prosseguindo com FadeOut. ex={ex.GetType().Name}: {ex.Message}");
            }
        }

        private static SceneTransitionContext BuildContext(SceneTransitionRequest request)
        {
            var loadList = NormalizeList(request.ScenesToLoad);
            var unloadList = NormalizeList(request.ScenesToUnload);
            return new SceneTransitionContext(loadList, unloadList, request.TargetActiveScene, request.UseFade,
                request.TransitionProfileName);
        }

        private static IReadOnlyList<string> NormalizeList(IReadOnlyList<string> source)
        {
            if (source == null)
            {
                return Array.Empty<string>();
            }

            return source
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Select(entry => entry.Trim())
                .ToArray();
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

            _fadeAdapter.ConfigureFromProfile(context.TransitionProfileName);
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
            await LoadScenesAsync(context.ScenesToLoad);

            await SetActiveSceneAsync(context.TargetActiveScene);

            await UnloadScenesAsync(context.ScenesToUnload, context.TargetActiveScene);
        }

        private async Task LoadScenesAsync(IReadOnlyList<string> scenesToLoad)
        {
            if (scenesToLoad == null || scenesToLoad.Count == 0)
            {
                return;
            }

            foreach (var sceneName in scenesToLoad)
            {
                if (_loaderAdapter.IsSceneLoaded(sceneName))
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

            var success = await _loaderAdapter.TrySetActiveSceneAsync(targetActiveScene);
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

            foreach (var sceneName in scenesToUnload)
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

        public void ConfigureFromProfile(string profileName) { }

        public Task FadeInAsync() => Task.CompletedTask;

        public Task FadeOutAsync() => Task.CompletedTask;
    }
}
