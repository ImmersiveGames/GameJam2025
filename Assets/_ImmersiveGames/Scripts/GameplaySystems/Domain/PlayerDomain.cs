using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlayerDomain : IPlayerDomain
    {
        private readonly List<IActor> _players = new();
        private readonly HashSet<string> _playerIds = new();

        public event Action<IActor> PlayerRegistered;
        public event Action<IActor> PlayerUnregistered;

        public IReadOnlyList<IActor> Players => _players;

        public bool RegisterPlayer(IActor actor)
        {
            if (actor == null)
                return false;

            // Importante: ActorId vazio pode acontecer durante inicialização.
            // Não é um erro em si; apenas "ainda não está pronto".
            if (string.IsNullOrWhiteSpace(actor.ActorId))
                return false;

            if (_playerIds.Contains(actor.ActorId))
                return false;

            _playerIds.Add(actor.ActorId);
            _players.Add(actor);

            PlayerRegistered?.Invoke(actor);

            DebugUtility.LogVerbose<PlayerDomain>(
                $"Player registrado no domínio: {actor.ActorName} ({actor.ActorId}).");

            return true;
        }

        public bool UnregisterPlayer(IActor actor)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
                return false;

            if (!_playerIds.Remove(actor.ActorId))
                return false;

            _players.RemoveAll(p => p == null || p.ActorId == actor.ActorId);

            PlayerUnregistered?.Invoke(actor);

            DebugUtility.LogVerbose<PlayerDomain>(
                $"Player removido do domínio: {actor.ActorName} ({actor.ActorId}).");

            return true;
        }

        public bool TryGetPlayerByIndex(int index, out IActor player)
        {
            player = null;

            if (index < 0 || index >= _players.Count)
                return false;

            player = _players[index];
            return player != null;
        }
    }
}
