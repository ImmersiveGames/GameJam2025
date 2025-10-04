using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.SkinSystems.Data;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
namespace _ImmersiveGames.Scripts.SkinSystems
{
    /// <summary>
    /// Evento para notificar mudanças de skin, usado com FilteredEventBus para direcionar atualizações a alvos específicos.
    /// </summary>
    public struct SkinUpdateEvent : IEvent
    {
        /// <summary>
        /// Configuração da skin a ser aplicada.
        /// </summary>
        public ISkinConfig SkinConfig { get; }

        /// <summary>
        /// Ator (IActor) que disparou a mudança de skin, usado para filtragem em multiplayer.
        /// </summary>
        public IActor Spawner { get; }

        public SkinUpdateEvent(ISkinConfig skinConfig, IActor spawner)
        {
            SkinConfig = skinConfig;
            Spawner = spawner;
        }
    }
}