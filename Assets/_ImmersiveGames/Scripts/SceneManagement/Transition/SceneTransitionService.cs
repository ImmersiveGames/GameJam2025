using System;
using System.Threading;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.SceneManagement.Hud;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa uma transição completa com base no contexto recebido.
        /// Pode ser cancelada via CancellationToken. Cancelamento é cooperativo
        /// e verificado entre fases (não interrompe AsyncOperations de Unity).
        /// </summary>
        Task RunTransitionAsync(SceneTransitionContext context, CancellationToken cancellationToken = default);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IFadeService _fadeService;
        private ISceneLoadingHudTaskService _hudService;

        /// <summary>
        /// Tempo mínimo (em segundos, tempo não escalado) que o HUD de loading
        /// deve permanecer visível para evitar "piscar".
        /// </summary>
        private const float MinHudVisibleSeconds = 0.5f;

        public SceneTransitionService(
            ISceneLoader sceneLoader,
            IFadeService fadeService)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _fadeService = fadeService;
        }

        private bool EnsureHudService()
        {
            if (_hudService != null)
                return true;

            var provider = DependencyManager.Provider;
            if (provider != null && provider.TryGetGlobal<ISceneLoadingHudTaskService>(out var hudService))
            {
                _hudService = hudService;
                DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Serviço de HUD localizado via DI.");
                return true;
            }

            DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Serviço de HUD NÃO localizado. Seguindo sem HUD.");
            return false;
        }

        public async Task RunTransitionAsync(SceneTransitionContext context, CancellationToken cancellationToken = default)
        {
            EnsureHudService();

            DebugUtility.Log<SceneTransitionService>(
                $"Iniciando transição...\nContexto = {context}",
                DebugUtility.Colors.Info);

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                EventBus<SceneTransitionStartedEvent>.Raise(
                    new SceneTransitionStartedEvent(context));

                // 1) FadeIn
                if (context.useFade && _fadeService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeIn...");
                    await _fadeService.FadeInAsync();
                }

                cancellationToken.ThrowIfCancellationRequested();

                float hudShownAt = -1f;

                // 2) HUD Show
                if (_hudService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Exibindo HUD de loading...");
                    await _hudService.ShowLoadingAsync(context);
                    hudShownAt = Time.unscaledTime;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 3) Load cenas alvo
                if (context.scenesToLoad != null && context.scenesToLoad.Count > 0)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Carregando cenas alvo: [{string.Join(", ", context.scenesToLoad)}]");

                    foreach (var sceneName in context.scenesToLoad)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        DebugUtility.LogVerbose<SceneTransitionService>(
                            $"[Scenes] Carregando cena '{sceneName}' (mode=Additive).");

                        await _sceneLoader.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // 4) Set active
                if (!string.IsNullOrWhiteSpace(context.targetActiveScene))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Definindo cena ativa: '{context.targetActiveScene}'");

                    await SetActiveSceneAsync(context.targetActiveScene);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 5) Unload cenas obsoletas
                if (context.scenesToUnload != null && context.scenesToUnload.Count > 0)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Descarregando cenas obsoletas: [{string.Join(", ", context.scenesToUnload)}]");

                    foreach (var sceneName in context.scenesToUnload)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        DebugUtility.LogVerbose<SceneTransitionService>(
                            $"[Scenes] Descarregando cena '{sceneName}'.");

                        await _sceneLoader.UnloadSceneAsync(sceneName);

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }

                // 6) HUD cenas prontas
                if (_hudService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Marcando cenas como prontas no HUD...");
                    await _hudService.MarkScenesReadyAsync(context);
                }

                EventBus<SceneTransitionScenesReadyEvent>.Raise(
                    new SceneTransitionScenesReadyEvent(context));

                cancellationToken.ThrowIfCancellationRequested();

                // 7) HUD Hide (com tempo mínimo de visibilidade)
                if (_hudService != null)
                {
                    if (hudShownAt > 0f)
                    {
                        float elapsed = Time.unscaledTime - hudShownAt;
                        if (elapsed < MinHudVisibleSeconds)
                        {
                            float remaining = MinHudVisibleSeconds - elapsed;
                            DebugUtility.LogVerbose<SceneTransitionService>(
                                $"[HUD] Aguardando {remaining:0.000}s para garantir tempo mínimo de HUD visível...");

                            int ms = Mathf.CeilToInt(remaining * 1000f);
                            await Task.Delay(ms, cancellationToken);
                        }
                    }

                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Ocultando HUD de loading...");
                    await _hudService.HideLoadingAsync(context);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // 8) FadeOut
                if (context.useFade && _fadeService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeOut...");
                    await _fadeService.FadeOutAsync();
                }

                EventBus<SceneTransitionCompletedEvent>.Raise(
                    new SceneTransitionCompletedEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    "Transição concluída.",
                    DebugUtility.Colors.Info);
            }
            catch (OperationCanceledException)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    "Transição cancelada via CancellationToken.");
                // Opcional: emitir algum evento específico de cancelamento, se quiser.
            }
        }

        private static async Task SetActiveSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' não é válida.");
                return;
            }

            if (!scene.isLoaded)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' ainda não está carregada.");
                return;
            }

            SceneManager.SetActiveScene(scene);
            await Task.Yield();
        }
    }
}
