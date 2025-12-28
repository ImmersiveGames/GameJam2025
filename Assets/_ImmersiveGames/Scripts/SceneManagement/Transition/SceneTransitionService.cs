using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
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

        public SceneTransitionService(
            ISceneLoader sceneLoader,
            IFadeService fadeService)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _fadeService = fadeService; // pode ser null (transição sem fade)
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
                }

                // 2) Load cenas alvo
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
                    }
                }

                // 3) Set active scene
                if (!string.IsNullOrWhiteSpace(context.targetActiveScene))
                {
                    DebugUtility.LogVerbose<SceneTransitionService>(
                        $"[Scenes] Definindo cena ativa: '{context.targetActiveScene}'");

                    await SetActiveSceneAsync(context.targetActiveScene);
                }

                // 4) Unload cenas obsoletas
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

                EventBus<SceneTransitionScenesReadyEvent>.Raise(
                    new SceneTransitionScenesReadyEvent(context));

                // 5) FadeOut
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
