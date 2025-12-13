using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Configs;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Hud;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa uma transição completa com base no contexto recebido.
        /// O fluxo é coordenado via Tasks (sem uso de CancellationToken).
        /// </summary>
        Task RunTransitionAsync(SceneTransitionContext context);
    }

    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IFadeService _fadeService;
        private ISceneLoadingHudTaskService _hudService;

        /// <summary>
        /// Tempo mínimo (em segundos, tempo não escalado) que o HUD de loading
        /// deve permanecer visível para evitar "piscar", caso o perfil não defina um valor.
        /// </summary>
        private const float DefaultMinHudVisibleSeconds = 0.5f;

        public SceneTransitionService(
            ISceneLoader sceneLoader,
            IFadeService fadeService)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _fadeService = fadeService; // pode ser null (transição sem fade)
        }

        private static float GetMinHudVisibleSeconds(SceneTransitionContext context)
        {
            var profile = context.transitionProfile;
            if (profile != null && profile.MinHudVisibleSeconds > 0f)
                return profile.MinHudVisibleSeconds;

            return DefaultMinHudVisibleSeconds;
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

        private void ConfigureFadeFromContext(SceneTransitionContext context)
        {
            // Mantém IFadeService “limpo”, mas aproveita a capacidade do FadeService concreto.
            // Se no futuro você quiser formalizar isso, aí sim criamos uma interface específica.
            if (_fadeService is FadeService fadeServiceConcrete)
            {
                fadeServiceConcrete.ConfigureFromProfile(context.transitionProfile);
            }
        }

        public async Task RunTransitionAsync(SceneTransitionContext context)
        {
            EnsureHudService();

            DebugUtility.Log<SceneTransitionService>(
                $"Iniciando transição...\nContexto = {context}",
                DebugUtility.Colors.Info);

            try
            {
                EventBus<SceneTransitionStartedEvent>.Raise(
                    new SceneTransitionStartedEvent(context));

                // Configura Fade com base no profile ANTES de qualquer FadeIn/Out.
                ConfigureFadeFromContext(context);

                // 1) FadeIn
                if (context.useFade && _fadeService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeIn...");
                    await _fadeService.FadeInAsync();

                    // Progresso inicial após concluir o FadeIn.
                    _hudService?.SetProgress(0.05f);
                }

                float hudShownAt = -1f;

                // 2) HUD Show
                if (_hudService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Exibindo HUD de loading...");
                    await _hudService.ShowLoadingAsync(context);
                    hudShownAt = Time.unscaledTime;

                    // Progresso após a HUD estar visível na tela.
                    _hudService.SetProgress(0.10f);
                }

                // 3) Load cenas alvo
                if (context.scenesToLoad != null && context.scenesToLoad.Count > 0)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Carregando cenas alvo: [{string.Join(", ", context.scenesToLoad)}]");

                    int totalToLoad = context.scenesToLoad.Count;
                    int loadedCount = 0;

                    foreach (var sceneName in context.scenesToLoad)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        DebugUtility.LogVerbose<SceneTransitionService>(
                            $"[Scenes] Carregando cena '{sceneName}' (mode=Additive).");

                        await _sceneLoader.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

                        loadedCount++;
                        float t = Mathf.Clamp01((float)loadedCount / totalToLoad);
                        float progress = Mathf.Lerp(0.10f, 0.90f, t);
                        _hudService?.SetProgress(progress);
                    }
                }

                // 4) Set active scene
                if (!string.IsNullOrWhiteSpace(context.targetActiveScene))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Definindo cena ativa: '{context.targetActiveScene}'");

                    await SetActiveSceneAsync(context.targetActiveScene);
                }

                // 5) Unload cenas obsoletas
                if (context.scenesToUnload is { Count: > 0 })
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
                    }
                }

                // 6) HUD cenas prontas
                if (_hudService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Marcando cenas como prontas no HUD...");
                    await _hudService.MarkScenesReadyAsync(context);

                    // Progresso quase final após as cenas estarem prontas.
                    _hudService.SetProgress(0.95f);
                }

                EventBus<SceneTransitionScenesReadyEvent>.Raise(
                    new SceneTransitionScenesReadyEvent(context));

                // 7) HUD Hide (com tempo mínimo de visibilidade)
                if (_hudService != null)
                {
                    if (hudShownAt > 0f)
                    {
                        float elapsed = Time.unscaledTime - hudShownAt;
                        float minVisible = GetMinHudVisibleSeconds(context);
                        if (elapsed < minVisible)
                        {
                            float remaining = minVisible - elapsed;
                            DebugUtility.LogVerbose<SceneTransitionService>(
                                $"[HUD] Aguardando {remaining:0.000}s para garantir tempo mínimo de HUD visível...");

                            int ms = Mathf.CeilToInt(remaining * 1000f);
                            await Task.Delay(ms);
                        }
                    }

                    DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Ocultando HUD de loading...");
                    await _hudService.HideLoadingAsync(context);
                }

                // 8) FadeOut
                if (context.useFade && _fadeService != null)
                {
                    DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeOut...");
                    await _fadeService.FadeOutAsync();

                    // Progresso final da transição.
                    _hudService?.SetProgress(1.0f);
                }

                EventBus<SceneTransitionCompletedEvent>.Raise(
                    new SceneTransitionCompletedEvent(context));

                DebugUtility.Log<SceneTransitionService>(
                    "Transição concluída.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<SceneTransitionService>(
                    $"Exceção durante transição de cenas: {ex}");
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
