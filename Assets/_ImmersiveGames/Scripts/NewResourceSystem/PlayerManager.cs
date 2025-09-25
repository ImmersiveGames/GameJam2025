using System.Collections.Generic;
using _ImmersiveGames.Scripts.NewResourceSystem.Interfaces;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.DependencySystems; // Adicionado para IActorRegistry
using UnityEngine;

namespace _ImmersiveGames.Scripts.NewResourceSystem
{
    public class PlayerManager : MonoBehaviour
    {
        public static PlayerManager Instance { get; private set; }
        
        [Inject] private IActorRegistry _actorRegistry; // Atualizado para interface segregada

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

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
}