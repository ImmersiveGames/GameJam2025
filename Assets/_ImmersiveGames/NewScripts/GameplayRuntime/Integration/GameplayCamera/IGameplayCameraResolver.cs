/*
 * ChangeLog
 * - Documentado contrato de resolu��o de c�mera com expectativa de fallback resiliente.
 */

using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.GameplayCamera
{
    public interface IGameplayCameraResolver
    {
        void RegisterCamera(int playerId, UnityEngine.Camera camera);
        void UnregisterCamera(int playerId, UnityEngine.Camera camera);
        UnityEngine.Camera GetCamera(int playerId);
        UnityEngine.Camera GetDefaultCamera();
        event Action<UnityEngine.Camera> OnDefaultCameraChanged;
        IReadOnlyDictionary<int, UnityEngine.Camera> AllCameras { get; }
    }
}


