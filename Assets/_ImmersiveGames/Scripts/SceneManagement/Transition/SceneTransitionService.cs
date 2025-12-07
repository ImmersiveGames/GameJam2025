using System;
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
        Task RunTransitionAsync(SceneTransitionContext context);
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
            _fadeService = fadeService; // pode ser null (sem fade)
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

        public async Task RunTransitionAsync(SceneTransitionContext context)
        {
            EnsureHudService();

            DebugUtility.Log<SceneTransitionService>(
                $"Iniciando transição...\nContexto = {context}",
                DebugUtility.Colors.Info);

            // 1) Notifica início da transição
            EventBus<SceneTransitionStartedEvent>.Raise(
                new SceneTransitionStartedEvent(context));

            // 2) FASE 1: FadeIn (escurece tudo primeiro)
            if (context.useFade && _fadeService != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeIn...");
                await _fadeService.FadeInAsync();
            }

            // Guardaremos aqui quando o HUD foi exibido, para garantir tempo mínimo de exibição.
            float hudShownAt = -1f;

            // 3) FASE 2: HUD - exibe overlay de loading sobre tela escura
            if (_hudService != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Exibindo HUD de loading...");
                await _hudService.ShowLoadingAsync(context);

                hudShownAt = Time.unscaledTime;
            }

            // 4) FASE 3: Carregamento das cenas alvo
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
                }
            }

            // 5) FASE 4: Define a cena ativa (se especificada)
            if (!string.IsNullOrWhiteSpace(context.targetActiveScene))
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"[Scenes] Definindo cena ativa: '{context.targetActiveScene}'");

                await SetActiveSceneAsync(context.targetActiveScene);
            }

            // 6) FASE 5: Descarrega as cenas obsoletas
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
                }
            }

            // 7) FASE 6: HUD - cenas prontas (texto "Finalizando carregamento...", 100%)
            if (_hudService != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Marcando cenas como prontas no HUD...");
                await _hudService.MarkScenesReadyAsync(context);
            }

            // 8) Notifica que as cenas já estão prontas
            EventBus<SceneTransitionScenesReadyEvent>.Raise(
                new SceneTransitionScenesReadyEvent(context));

            // 9) FASE 7: HUD - oculta overlay de loading ANTES do fade-out,
            // garantindo um tempo mínimo de exibição.
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

                        // Task.Delay usa tempo de relógio; como é curto, é suficiente para suavizar a percepção.
                        int ms = Mathf.CeilToInt(remaining * 1000f);
                        await Task.Delay(ms);
                    }
                }

                DebugUtility.LogVerbose<SceneTransitionService>("[HUD] Ocultando HUD de loading...");
                await _hudService.HideLoadingAsync(context);
            }

            // 10) FASE 8: FadeOut (revela as novas cenas sem HUD)
            if (context.useFade && _fadeService != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>("[Fade] Executando FadeOut...");
                await _fadeService.FadeOutAsync();
            }

            // 11) Notifica conclusão da transição
            EventBus<SceneTransitionCompletedEvent>.Raise(
                new SceneTransitionCompletedEvent(context));

            DebugUtility.Log<SceneTransitionService>(
                "Transição concluída.",
                DebugUtility.Colors.Info);
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
