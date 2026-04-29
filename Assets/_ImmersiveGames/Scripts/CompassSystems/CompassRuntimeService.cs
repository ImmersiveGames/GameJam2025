using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Foundation.Core.Logging;
using _ImmersiveGames.NewScripts.Foundation.Platform.Composition;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// ServiÃ§o de runtime da bÃºssola registrado via DependencyManager (escopo global).
    /// Evita uso de classe estÃ¡tica pura, mas mantÃ©m um fallback seguro para cenas
    /// que ainda nÃ£o inicializaram o pipeline de injeÃ§Ã£o. MantÃ©m player e trackables.
    /// </summary>
    public sealed class CompassRuntimeService : MonoBehaviour, ICompassRuntimeService
    {
        private static CompassRuntimeService _instance;
        private readonly List<ICompassTrackable> _trackables = new();

        /// <summary>
        /// Transform do jogador utilizado como referÃªncia de direÃ§Ã£o na bÃºssola.
        /// </summary>
        public Transform PlayerTransform { get; private set; }

        /// <summary>
        /// Lista somente leitura de alvos rastreÃ¡veis registrados (ordem de registro).
        /// </summary>
        public IReadOnlyList<ICompassTrackable> Trackables => _trackables;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
#if NEWSCRIPTS_MODE
            DebugUtility.Log(typeof(CompassRuntimeService), "NEWSCRIPTS_MODE ativo: CompassRuntimeService ignorado.");
            #else
            // Garante que exista uma instÃ¢ncia viva antes das cenas serem carregadas.
            if (_instance != null)
            {
                return;
            }

            var runtimeRoot = new GameObject("CompassRuntimeService");
            DontDestroyOnLoad(runtimeRoot);
            runtimeRoot.AddComponent<CompassRuntimeService>();
#endif
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(this);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterInDependencyManager();
        }

        private void RegisterInDependencyManager()
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning<CompassRuntimeService>(
                    "DependencyManager.Provider indisponÃ­vel; serviÃ§o ficarÃ¡ acessÃ­vel apenas via fallback estÃ¡tico.",
                    this);
                return;
            }

            DependencyManager.Provider.RegisterGlobal<ICompassRuntimeService>(this);
            DebugUtility.Log<CompassRuntimeService>(
                "CompassRuntimeService registrado no escopo global (DependencyManager).",
                DebugUtility.Colors.Success,
                this);
        }

        /// <summary>
        /// Tenta recuperar o serviÃ§o a partir do DependencyManager ou da instÃ¢ncia ativa.
        /// </summary>
        public static bool TryGet(out ICompassRuntimeService service)
        {
            if (DependencyManager.Provider != null && DependencyManager.Provider.TryGetGlobal(out service))
            {
                return true;
            }

            service = _instance;
            return service != null;
        }

        /// <summary>
        /// Define o transform do jogador atual.
        /// </summary>
        /// <param name="playerTransform">Transform do jogador.</param>
        public void SetPlayer(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        /// <summary>
        /// Remove a referÃªncia do jogador se corresponder ao transform informado.
        /// </summary>
        /// <param name="playerTransform">Transform associado ao jogador a ser removido.</param>
        public void ClearPlayer(Transform playerTransform)
        {
            if (PlayerTransform == playerTransform)
            {
                PlayerTransform = null;
            }
        }

        /// <summary>
        /// Registra um alvo rastreÃ¡vel na bÃºssola.
        /// </summary>
        /// <param name="target">InstÃ¢ncia do alvo.</param>
        public void RegisterTarget(ICompassTrackable target)
        {
            if (target == null)
            {
                return;
            }

            RemoveNullEntries();

            if (_trackables.Contains(target))
            {
                return;
            }

            _trackables.Add(target);

            DebugUtility.LogVerbose<CompassRuntimeService>(
                $"ðŸŽ¯ Trackable registrado na bÃºssola: {target.Transform?.name ?? target.ToString()}",
                DebugUtility.Colors.Success);
        }

        /// <summary>
        /// Remove um alvo rastreÃ¡vel previamente registrado.
        /// </summary>
        /// <param name="target">InstÃ¢ncia do alvo.</param>
        public void UnregisterTarget(ICompassTrackable target)
        {
            if (target == null)
            {
                return;
            }

            if (_trackables.Remove(target))
            {
                DebugUtility.LogVerbose<CompassRuntimeService>(
                    $"ðŸ§­ Trackable removido da bÃºssola: {target.Transform?.name ?? target.ToString()}",
                    DebugUtility.Colors.Error);
            }

            RemoveNullEntries();
        }

        /// <summary>
        /// Remove entradas nulas que possam ter ficado na lista apÃ³s destruiÃ§Ã£o de objetos Unity.
        /// </summary>
        private void RemoveNullEntries()
        {
            for (int i = _trackables.Count - 1; i >= 0; i--)
            {
                if (_trackables[i] == null)
                {
                    _trackables.RemoveAt(i);
                }
            }
        }
    }
}

