using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.World.Compass
{
    /// <summary>
    /// Serviço estático que centraliza dados da bússola em tempo de execução.
    /// Mantém referência ao jogador e aos alvos rastreáveis, permitindo que
    /// sistemas de HUD acessem essas informações sem dependências diretas na cena.
    /// </summary>
    public static class CompassRuntimeService
    {
        private static readonly List<ICompassTrackable> _trackables = new();

        /// <summary>
        /// Transform do jogador utilizado como referência de direção na bússola.
        /// </summary>
        public static Transform PlayerTransform { get; private set; }

        /// <summary>
        /// Lista somente leitura de alvos rastreáveis registrados.
        /// </summary>
        public static IReadOnlyList<ICompassTrackable> Trackables => _trackables;

        /// <summary>
        /// Define o transform do jogador atual.
        /// </summary>
        /// <param name="playerTransform">Transform do jogador.</param>
        public static void SetPlayer(Transform playerTransform)
        {
            PlayerTransform = playerTransform;
        }

        /// <summary>
        /// Remove a referência do jogador se corresponder ao transform informado.
        /// </summary>
        /// <param name="playerTransform">Transform associado ao jogador a ser removido.</param>
        public static void ClearPlayer(Transform playerTransform)
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
        public static void RegisterTarget(ICompassTrackable target)
        {
            if (target == null || _trackables.Contains(target))
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
        public static void UnregisterTarget(ICompassTrackable target)
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
        }
    }
}
