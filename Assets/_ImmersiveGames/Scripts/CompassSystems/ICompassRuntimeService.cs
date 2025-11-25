using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.Scripts.CompassSystems
{
    /// <summary>
    /// Contrato para o serviço de runtime da bússola exposto via DependencyManager.
    /// Mantém referências a player e trackables para consumo da HUD e adaptadores.
    /// </summary>
    public interface ICompassRuntimeService
    {
        Transform PlayerTransform { get; }
        IReadOnlyList<ICompassTrackable> Trackables { get; }

        void SetPlayer(Transform playerTransform);
        void ClearPlayer(Transform playerTransform);
        void RegisterTarget(ICompassTrackable target);
        void UnregisterTarget(ICompassTrackable target);
    }
}
