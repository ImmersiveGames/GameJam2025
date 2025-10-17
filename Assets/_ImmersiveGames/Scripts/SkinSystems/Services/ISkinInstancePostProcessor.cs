using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Define etapas adicionais após a criação de uma instância de skin.
    /// Mantém o <see cref="SkinService"/> aberto a extensões sem modificações diretas.
    /// </summary>
    public interface ISkinInstancePostProcessor
    {
        void Process(GameObject instance, ISkinConfig config, IActor owner);
    }
}
