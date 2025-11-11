using System.Collections.Generic;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
using UnityUtils;
namespace _ImmersiveGames.Scripts.GameManagerSystems
{
    [DefaultExecutionOrder(-90)]
    public sealed class PlayerManager : Singleton<PlayerManager>
    {
        [SerializeField] private List<Transform> players = new();
        public IReadOnlyList<Transform> Players => players.AsReadOnly();

        protected override void Awake()
        {
            base.Awake();
            InitializePlayers();
        }

        private void InitializePlayers()
        {
            // Lógica para inicializar jogadores (ex.: configurar controles, spawns, etc.)
            foreach (var player in players)
            {
                if (player == null)
                {
                    DebugUtility.LogWarning<PlayerManager>("Jogador nulo detectado na lista de jogadores.", this);
                    continue;
                }
                // Exemplo: Configurar controles ou estado inicial
                DebugUtility.LogVerbose<PlayerManager>($"Jogador {player.name} inicializado.");
            }
        }

        public void AddPlayer(Transform player)
        {
            if (player != null && !players.Contains(player))
            {
                players.Add(player);
                DebugUtility.LogVerbose<PlayerManager>($"Jogador {player.name} adicionado.");
            }
        }

        public void RemovePlayer(Transform player)
        {
            if (players.Remove(player))
            {
                DebugUtility.LogVerbose<PlayerManager>($"Jogador {player.name} removido.");
            }
        }
    }
}