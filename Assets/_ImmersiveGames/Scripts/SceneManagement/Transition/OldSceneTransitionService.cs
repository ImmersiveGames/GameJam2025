using System;
using System.Threading.Tasks;
using _ImmersiveGames.Scripts.LegadoFadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.NewScripts.Core.Events;
using _ImmersiveGames.NewScripts.Core.Logging;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.OldTransition
{
    public interface IOldSceneTransitionService
    {
        /// <summary>
        /// Executa uma transi��o completa com base no contexto recebido.
        /// O fluxo � coordenado via Tasks (sem uso de CancellationToken).
        /// </summary>
        Task RunTransitionAsync(SceneTransitionContext context);
    }

    public sealed class OldSceneTransitionService : IOldSceneTransitionService
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly ILegadoFadeService _legadoFadeService;

        public OldSceneTransitionService(
            ISceneLoader sceneLoader,
            ILegadoFadeService legadoFadeService)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _legadoFadeService = legadoFadeService; // pode ser null (transi��o sem fade)
        }

        private void ConfigureFadeFromContext(SceneTransitionContext context)
        {
            // Mant�m ILegadoFadeService �limpo�, mas aproveita a capacidade do LegadoFadeService concreto.
            // Se no futuro voc� quiser formalizar isso, a� sim criamos uma interface espec�fica.
            if (_legadoFadeService is LegadoFadeService fadeServiceConcrete)
            {
                fadeServiceConcrete.ConfigureFromProfile(context.transitionProfile);
            }
        }

        public async Task RunTransitionAsync(SceneTransitionContext context)
        {
            DebugUtility.Log<OldSceneTransitionService>(
                $"Iniciando transi��o...\nContexto = {context}",
                DebugUtility.Colors.Info);

            try
            {
                EventBus<SceneTransitionStartedEvent>.Raise(
                    new SceneTransitionStartedEvent(context));

                // Configura Fade com base no profile ANTES de qualquer FadeIn/Out.
                ConfigureFadeFromContext(context);

                // 1) FadeIn
                if (context.useFade && _legadoFadeService != null)
                {
                    DebugUtility.LogVerbose<OldSceneTransitionService>("[Fade] Executando FadeIn...");
                    await _legadoFadeService.FadeInAsync();
                }

                // 2) Load cenas alvo
                if (context.scenesToLoad != null && context.scenesToLoad.Count > 0)
                {
                    DebugUtility.LogVerbose<OldSceneTransitionService>(
                        $"[Scenes] Carregando cenas alvo: [{string.Join(", ", context.scenesToLoad)}]");

                    int totalToLoad = context.scenesToLoad.Count;
                    int loadedCount = 0;

                    foreach (var sceneName in context.scenesToLoad)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        DebugUtility.LogVerbose<OldSceneTransitionService>(
                            $"[Scenes] Carregando cena '{sceneName}' (mode=Additive).");

                        await _sceneLoader.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                        loadedCount++;
                    }
                }

                // 3) Set active scene
                if (!string.IsNullOrWhiteSpace(context.targetActiveScene))
                {
                    DebugUtility.LogVerbose<OldSceneTransitionService>(
                        $"[Scenes] Definindo cena ativa: '{context.targetActiveScene}'");

                    await SetActiveSceneAsync(context.targetActiveScene);
                }

                // 4) Unload cenas obsoletas
                if (context.scenesToUnload is { Count: > 0 })
                {
                    DebugUtility.LogVerbose<OldSceneTransitionService>(
                        $"[Scenes] Descarregando cenas obsoletas: [{string.Join(", ", context.scenesToUnload)}]");

                    foreach (var sceneName in context.scenesToUnload)
                    {
                        if (string.IsNullOrWhiteSpace(sceneName))
                            continue;

                        DebugUtility.LogVerbose<OldSceneTransitionService>(
                            $"[Scenes] Descarregando cena '{sceneName}'.");

                        await _sceneLoader.UnloadSceneAsync(sceneName);
                    }
                }

                EventBus<SceneTransitionScenesReadyEvent>.Raise(
                    new SceneTransitionScenesReadyEvent(context));

                // 5) FadeOut
                if (context.useFade && _legadoFadeService != null)
                {
                    DebugUtility.LogVerbose<OldSceneTransitionService>("[Fade] Executando FadeOut...");
                    await _legadoFadeService.FadeOutAsync();
                }

                EventBus<SceneTransitionCompletedEvent>.Raise(
                    new SceneTransitionCompletedEvent(context));

                DebugUtility.Log<OldSceneTransitionService>(
                    "Transi��o conclu�da.",
                    DebugUtility.Colors.Info);
            }
            catch (Exception ex)
            {
                DebugUtility.LogError<OldSceneTransitionService>(
                    $"Exce��o durante transi��o de cenas: {ex}");
            }
        }

        private static async Task SetActiveSceneAsync(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning<OldSceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' n�o � v�lida.");
                return;
            }

            if (!scene.isLoaded)
            {
                DebugUtility.LogWarning<OldSceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' ainda n�o est� carregada.");
                return;
            }

            SceneManager.SetActiveScene(scene);
            await Task.Yield();
        }
    }
}


