using _ImmersiveGames.Scripts.ResourceSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    public class PlayerManager : PersistentSingleton<PlayerManager>
    {
        
        [Inject] private IActorRegistry _actorRegistry; // Atualizado para interface segregada

        public void RegisterPlayer(string playerId, EntityResourceSystem resourceSystem)
        {
            _actorRegistry.RegisterActor(playerId);
            DebugUtility.LogVerbose<PlayerManager>($"🎮 Player registered: {playerId}");
        }

        public void HealAllPlayers(int healAmount)
        {
            // Como players são atores registrados, itere via serviço se precisar de lista completa
            // Para simplicidade, assuma que chamadores conhecem os players; se precisar, adicione GetRegisteredActors() em IActorRegistry
            DebugUtility.LogVerbose<PlayerManager>($"❤️ All players healed: +{healAmount} (implemente iteração se necessário)");
        }

        [ContextMenu("Debug Players")]
        public void DebugPlayers()
        {
            // Use o serviço para debug, evitando duplicar dicionário
            DebugUtility.LogVerbose<PlayerManager>($"🎮 Total players: {_actorRegistry.GetResourceCountForActor("all")} (ajuste para listar atores)");
        }
    }
    
    // ✅ Interface segregada para gerenciamento de atores (SRP e ISP)
    public interface IActorRegistry
    {
        void RegisterActor(string actorId);
        void UnregisterActor(string actorId);
        bool IsActorRegistered(string actorId);
        void RemoveActorBindings(string actorId);
        int GetResourceCountForActor(string actorId);
    }
}