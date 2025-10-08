using _ImmersiveGames.Scripts.AudioSystem.Configs;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.AudioSystem
{
    public class GameAudioManager : Singleton<GameAudioManager>
    {
        [SerializeField] private SoundData mainMenuBGM;
        [Header("Settings")]
        [SerializeField] private float bgmFadeDuration = 2f;
        
        private void Start()
        {
            AudioSystemHelper.PlayBGM(mainMenuBGM, loop: true, bgmFadeDuration);
            AudioSystemHelper.SetBGMVolume(1f);
  
        }
    }
}