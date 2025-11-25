using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// Serviço de runtime da bússola registrado via DependencyManager (escopo global).
    /// Evita uso de classe estática pura, mas mantém um fallback seguro para cenas
    /// que ainda não inicializaram o pipeline de injeção. Mantém player e trackables.
    /// </summary>
    public sealed class CompassRuntimeService : MonoBehaviour, ICompassRuntimeService
    {
        private static CompassRuntimeService _instance;
        private readonly List<ICompassTrackable> _trackables = new();

        /// <summary>
        /// Transform do jogador utilizado como referência de direção na bússola.
        /// </summary>
        public Transform PlayerTransform { get; private set; }

        /// <summary>
        /// Lista somente leitura de alvos rastreáveis registrados (ordem de registro).
        /// </summary>
        public IReadOnlyList<ICompassTrackable> Trackables => _trackables;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            // Garante que exista uma instância viva antes das cenas serem carregadas.
            if (_instance != null)
            {
                return;
            }

            var runtimeRoot = new GameObject("CompassRuntimeService");
            DontDestroyOnLoad(runtimeRoot);
            runtimeRoot.AddComponent<CompassRuntimeService>();
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
                    "DependencyManager.Provider indisponível; serviço ficará acessível apenas via fallback estático.",
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
        /// Tenta recuperar o serviço a partir do DependencyManager ou da instância ativa.
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
        /// Remove a referência do jogador se corresponder ao transform informado.
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
        /// Registra um alvo rastreável na bússola.
        /// </summary>
        /// <param name="target">Instância do alvo.</param>
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
                $"🎯 Trackable registrado na bússola: {target.Transform?.name ?? target.ToString()}",
                DebugUtility.Colors.Success);
        }

        /// <summary>
        /// Remove um alvo rastreável previamente registrado.
        /// </summary>
        /// <param name="target">Instância do alvo.</param>
        public void UnregisterTarget(ICompassTrackable target)
        {
            if (target == null)
            {
                return;
            }

            if (_trackables.Remove(target))
            {
                DebugUtility.LogVerbose<CompassRuntimeService>(
                    $"🧭 Trackable removido da bússola: {target.Transform?.name ?? target.ToString()}",
                    DebugUtility.Colors.Error);
            }

            RemoveNullEntries();
        }

        /// <summary>
        /// Remove entradas nulas que possam ter ficado na lista após destruição de objetos Unity.
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
