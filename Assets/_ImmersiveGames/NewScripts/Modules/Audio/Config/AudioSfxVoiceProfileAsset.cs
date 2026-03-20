using _ImmersiveGames.NewScripts.Infrastructure.Pooling.Config;
using UnityEngine;
using UnityEngine.Serialization;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    [CreateAssetMenu(
        fileName = "AudioSfxVoiceProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Voice Profile",
        order = 2)]
    public sealed class AudioSfxVoiceProfileAsset : ScriptableObject
    {
        [FormerlySerializedAs("pooledVoicePool")]
        [SerializeField] private PoolDefinitionAsset pooledVoicePoolDefinition;
        [SerializeField] private bool allowDirectFallback = true;
        [SerializeField] [Min(0)] private int defaultVoiceBudget = 16;
        [SerializeField] [Min(0f)] private float releaseGraceSeconds = 0.05f;

        public PoolDefinitionAsset PooledVoicePoolDefinition => pooledVoicePoolDefinition;
        public bool AllowDirectFallback => allowDirectFallback;
        public int DefaultVoiceBudget => defaultVoiceBudget;
        public float ReleaseGraceSeconds => releaseGraceSeconds;
    }
}
