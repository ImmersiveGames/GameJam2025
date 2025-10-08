using UnityEngine;
namespace _ImmersiveGames.Scripts.AudioSystem.Configs
{
    [CreateAssetMenu(menuName = "ImmersiveGames/Audio/Audio Service Settings")]
    public class AudioServiceSettings : ScriptableObject
    {
        [Header("Pool Names / Settings")]
        public string soundEmitterPoolName = "SoundEmitterPool";

        [Header("Debug")]
        public bool debugEmitters = false;
        public bool verboseLogs = false;
    }
}