using _ImmersiveGames.Scripts.AudioSystem.Configs;

namespace _ImmersiveGames.Scripts.AudioSystem.Interfaces
{
    /// <summary>
    /// Serviço dedicado a encapsular as regras de cálculo de volume.
    /// Mantém a lógica de composição das camadas em um ponto único,
    /// preservando SRP e facilitando extensões futuras (ex.: novos multiplicadores).
    /// </summary>
    public interface IAudioVolumeService
    {
        float CalculateBgmVolume(SoundData soundData, AudioServiceSettings settings, float contextMultiplier = 1f);
        float CalculateSfxVolume(SoundData soundData, AudioConfig config, AudioServiceSettings settings, AudioContext context);
    }
}
