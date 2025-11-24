using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Serviço de runtime da bússola registrado via DependencyManager (escopo global).
    /// Evita uso de classe estática para respeitar o pipeline de injeção do projeto,
    /// mantendo a responsabilidade de rastrear player e alvos.
    /// </summary>
    public sealed class CompassRuntimeService : ICompassRuntimeService
    {
        private static CompassRuntimeService _cachedInstance;
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
        private static void Initialize()
        {
            EnsureInstance();
        }

        /// <summary>
        /// Tenta recuperar o serviço a partir do DependencyManager ou do cache local.
        /// </summary>
        public static bool TryGet(out ICompassRuntimeService service)
        {
            if (DependencyManager.Provider != null && DependencyManager.Provider.TryGetGlobal(out service))
            {
                return true;
            }

            service = _cachedInstance;
            return service != null;
        }

        /// <summary>
        /// Obtém o serviço, lançando exceção se não estiver disponível. Útil para pontos críticos.
        /// </summary>
        public static ICompassRuntimeService Require()
        {
            if (TryGet(out ICompassRuntimeService service))
            {
                return service;
            }

            throw new InvalidOperationException("CompassRuntimeService não foi inicializado pelo DependencyManager.");
        }

        private static void EnsureInstance()
        {
            if (_cachedInstance != null)
            {
                return;
            }

            _cachedInstance = new CompassRuntimeService();
            RegisterGlobal(_cachedInstance);
        }

        private static void RegisterGlobal(ICompassRuntimeService service)
        {
            if (DependencyManager.Provider == null)
            {
                DebugUtility.LogWarning<CompassRuntimeService>(
                    "DependencyManager.Provider indisponível; serviço será usado apenas via cache local.");
                return;
            }

            DependencyManager.Provider.RegisterGlobal(service);
            DebugUtility.Log<CompassRuntimeService>(
                "CompassRuntimeService registrado no escopo global (DependencyManager).",
                DebugUtility.Colors.Success);
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
                $"🎯 Trackable registrado na bússola: {target.Transform?.name ?? target.ToString()}");
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
                    $"🧭 Trackable removido da bússola: {target.Transform?.name ?? target.ToString()}");
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
