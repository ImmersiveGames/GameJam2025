using System;
namespace _ImmersiveGames.NewScripts.Experience.Audio.Runtime
{
    /// <summary>
    /// Estado runtime de volume/sessão do jogador.
    /// Não representa defaults do projeto.
    /// </summary>
    public interface IAudioSettingsService
    {
        event Action<string> VolumeChanged;

        float MasterVolume { get; set; }
        float BgmVolume { get; set; }
        float SfxVolume { get; set; }
        float BgmCategoryMultiplier { get; set; }
        float SfxCategoryMultiplier { get; set; }
    }
}
