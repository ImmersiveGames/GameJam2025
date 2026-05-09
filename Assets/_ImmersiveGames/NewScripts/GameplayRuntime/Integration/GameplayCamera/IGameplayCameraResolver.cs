/*
 * ChangeLog
 * - Documentado contrato de resolu��o de c�mera com expectativa de fallback resiliente.
 */

using System;
using System.Collections.Generic;
using UnityEngine;
namespace _ImmersiveGames.NewScripts.GameplayRuntime.Integration.GameplayCamera
{
    public interface IGameplayCameraResolver
    {
        void RegisterCamera(int playerId, Camera camera);
        void UnregisterCamera(int playerId, Camera camera);
        Camera GetCamera(int playerId);
        Camera GetDefaultCamera();
        event Action<Camera> OnDefaultCameraChanged;
        IReadOnlyDictionary<int, Camera> AllCameras { get; }
    }
}


