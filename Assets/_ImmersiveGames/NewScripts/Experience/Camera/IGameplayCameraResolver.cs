/*
 * ChangeLog
 * - Documentado contrato de resolução de câmera com expectativa de fallback resiliente.
 */

using System;
using System.Collections.Generic;
namespace _ImmersiveGames.NewScripts.Modules.Gameplay.Camera
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
