using System.Threading.Tasks;
using _ImmersiveGames.Scripts.FadeSystem;
using _ImmersiveGames.Scripts.SceneManagement.Core;
using _ImmersiveGames.Scripts.SceneManagement.Events;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine.SceneManagement;

namespace _ImmersiveGames.Scripts.SceneManagement.Transition
{
    public interface ISceneTransitionService
    {
        /// <summary>
        /// Executa uma transição completa com base no contexto recebido:
        /// - (Opcional) FadeIn
        /// - Load cenas alvo (Additive)
        /// - Set active scene
        /// - Unload cenas obsoletas
        /// - (Opcional) FadeOut
        /// </summary>
        Task RunTransitionAsync(SceneTransitionContext context);
    }

    /// <summary>
    /// Implementação padrão do serviço de transição de cenas.
    /// Usa ISceneLoader para carregar/descarregar e um IFadeAwaiter opcional
    /// para coordenar o fade sem usar corrotinas.
    /// </summary>
    public sealed class SceneTransitionService : ISceneTransitionService
    {
        private readonly ISceneLoader _sceneLoader;
        private readonly IFadeAwaiter _fadeAwaiter;

        public SceneTransitionService(ISceneLoader sceneLoader, IFadeAwaiter fadeAwaiter)
        {
            _sceneLoader = sceneLoader;
            _fadeAwaiter = fadeAwaiter;
        }

        public async Task RunTransitionAsync(SceneTransitionContext context)
        {
            DebugUtility.LogVerbose<SceneTransitionService>(
                "Iniciando transição...");
            DebugUtility.LogVerbose<SceneTransitionService>(
                "Contexto recebido: " + context);

            // Notifica início da transição (antes de qualquer efeito visual ou load/unload)
            EventBus<SceneTransitionStartedEvent>.Raise(
                new SceneTransitionStartedEvent(context));

            // 1) FadeIn opcional
            if (context.useFade && _fadeAwaiter != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    "Executando FadeIn...");
                await _fadeAwaiter.FadeInAsync();
            }

            // 2) Carregar cenas alvo (sempre Additive)
            foreach (var sceneName in context.scenesToLoad)
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"Carregando cena alvo: {sceneName}");
                await _sceneLoader.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }

            // 3) Definir cena ativa, se houver alvo definido
            if (!string.IsNullOrEmpty(context.targetActiveScene))
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"Definindo cena ativa: {context.targetActiveScene}");
                await SetActiveSceneAsync(context.targetActiveScene);
            }

            // 4) Descarregar cenas obsoletas
            foreach (var sceneName in context.scenesToUnload)
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    $"Descarregando cena obsoleta: {sceneName}");
                await _sceneLoader.UnloadSceneAsync(sceneName);
            }

            // Neste ponto:
            // - Todas as cenas alvo estão carregadas
            // - Todas as cenas obsoletas foram descarregadas
            // - A cena ativa já foi definida
            EventBus<SceneTransitionScenesReadyEvent>.Raise(
                new SceneTransitionScenesReadyEvent(context));

            // 5) FadeOut opcional
            if (context.useFade && _fadeAwaiter != null)
            {
                DebugUtility.LogVerbose<SceneTransitionService>(
                    "Executando FadeOut...");
                await _fadeAwaiter.FadeOutAsync();
            }

            DebugUtility.LogVerbose<SceneTransitionService>(
                "Transição concluída.");

            // 6) Notifica conclusão total (incluindo FadeOut)
            EventBus<SceneTransitionCompletedEvent>.Raise(
                new SceneTransitionCompletedEvent(context));
        }

        /// <summary>
        /// Define a cena ativa usando SceneManager, sem depender de métodos extras no ISceneLoader.
        /// Mantém o mesmo modelo assíncrono (Task) usando Task. Yield apenas para cooperar com o loop.
        /// </summary>
        private static async Task SetActiveSceneAsync(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' não encontrada ou inválida.");
                return;
            }

            if (!scene.isLoaded)
            {
                DebugUtility.LogWarning<SceneTransitionService>(
                    $"SetActiveSceneAsync: cena '{sceneName}' ainda não está carregada.");
                return;
            }

            SceneManager.SetActiveScene(scene);

            // Pequena rendição para manter a semântica assíncrona
            await Task.Yield();
        }
    }
}