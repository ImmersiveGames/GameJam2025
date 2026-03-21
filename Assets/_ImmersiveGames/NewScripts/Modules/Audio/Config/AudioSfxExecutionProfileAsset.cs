using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    [CreateAssetMenu(
        fileName = "AudioSfxExecutionProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Execution Profile",
        order = 4)]
    public sealed class AudioSfxExecutionProfileAsset : ScriptableObject
    {
        [SerializeField] private AudioSfxExecutionMode executionMode = AudioSfxExecutionMode.DirectOneShot;
        [SerializeField] private AudioSfxVoiceProfileAsset pooledVoiceProfile;

        public AudioSfxExecutionMode ExecutionMode => executionMode;
        public AudioSfxVoiceProfileAsset PooledVoiceProfile => pooledVoiceProfile;
    }
}
