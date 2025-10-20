using UnityEngine;
using _ImmersiveGames.Scripts.AudioSystem.Base;
using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem.Components
{
    /// <summary>
    /// Reproduz efeitos sonoros para explosões instanciadas via pool.
    /// Pode utilizar um pool local de SoundEmitter para minimizar alocações.
    /// </summary>
    public class ExplosionAudioController : AudioControllerBase
    {
        [Header("Explosion Sound")]
        [SerializeField] private SoundData explosionSound;

        /// <summary>
        /// Toca o som configurado na posição informada utilizando o AudioSystem.
        /// </summary>
        public void PlayExplosionSound(Vector3 position, float volumeMultiplier = 1f)
        {
            if (explosionSound == null)
            {
                return;
            }

            bool useSpatial = audioConfig?.useSpatialBlend ?? true;
            var context = AudioContext.Default(position, useSpatial, volumeMultiplier);
            PlaySoundLocal(explosionSound, context);
        }

        /// <summary>
        /// Permite alterar o som dinamicamente caso múltiplas explosões compartilhem o mesmo prefab.
        /// </summary>
        public void SetExplosionSound(SoundData sound)
        {
            explosionSound = sound;
        }
    }
}
