using UnityEngine;

namespace _ImmersiveGames.NewScripts.Modules.Audio.Config
{
    [CreateAssetMenu(
        fileName = "AudioSfxEmissionProfile",
        menuName = "ImmersiveGames/NewScripts/Audio/Audio SFX Emission Profile",
        order = 3)]
    public sealed class AudioSfxEmissionProfileAsset : ScriptableObject
    {
        [SerializeField] private AudioSfxPlaybackMode emissionMode = AudioSfxPlaybackMode.Global;
        [SerializeField] [Range(0f, 1f)] private float spatialBlend = 1f;
        [SerializeField] [Min(0f)] private float minDistance = 1f;
        [SerializeField] [Min(0f)] private float maxDistance = 40f;

        public AudioSfxPlaybackMode EmissionMode => emissionMode;
        public float SpatialBlend => spatialBlend;
        public float MinDistance => minDistance;
        public float MaxDistance => maxDistance;
    }
}
