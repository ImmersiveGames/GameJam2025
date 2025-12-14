using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IPlayerDomain
    {
        event Action<IActor> PlayerRegistered;
        event Action<IActor> PlayerUnregistered;

        IReadOnlyList<IActor> Players { get; }

        bool RegisterPlayer(IActor actor);
        bool UnregisterPlayer(IActor actor);
        bool TryGetPlayerByIndex(int index, out IActor player);

        // NEW: spawn pose registry
        bool TryGetSpawnPose(string actorId, out Pose pose);
        void SetSpawnPose(string actorId, Pose pose);
    }

    [DebugLevel(DebugLevel.Verbose)]
    public sealed class PlayerDomain : IPlayerDomain
    {
        private readonly List<IActor> _players = new();
        private readonly HashSet<string> _playerIds = new();

        // NEW: Spawn pose por ActorId (capturado no primeiro register, ou atualizado via SetSpawnPose)
        private readonly Dictionary<string, Pose> _spawnPoses = new();

        public event Action<IActor> PlayerRegistered;
        public event Action<IActor> PlayerUnregistered;

        public IReadOnlyList<IActor> Players => _players;

        public bool RegisterPlayer(IActor actor)
        {
            if (actor == null)
                return false;

            if (string.IsNullOrWhiteSpace(actor.ActorId))
                return false;

            if (_playerIds.Contains(actor.ActorId))
                return false;

            _playerIds.Add(actor.ActorId);
            _players.Add(actor);

            // Captura spawn pose inicial (somente se ainda não existe)
            if (!_spawnPoses.ContainsKey(actor.ActorId) && actor.Transform != null)
            {
                var t = actor.Transform;
                _spawnPoses[actor.ActorId] = new Pose(t.position, t.rotation);

                DebugUtility.LogVerbose<PlayerDomain>(
                    $"SpawnPose capturado para Player '{actor.ActorName}' ({actor.ActorId}) => pos={t.position}, rot={t.rotation.eulerAngles}.");
            }

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

        public bool TryGetSpawnPose(string actorId, out Pose pose)
        {
            pose = default;

            if (string.IsNullOrWhiteSpace(actorId))
                return false;

            return _spawnPoses.TryGetValue(actorId, out pose);
        }

        public void SetSpawnPose(string actorId, Pose pose)
        {
            if (string.IsNullOrWhiteSpace(actorId))
                return;

            _spawnPoses[actorId] = pose;

            DebugUtility.LogVerbose<PlayerDomain>(
                $"SpawnPose atualizado para ActorId='{actorId}' => pos={pose.position}, rot={pose.rotation.eulerAngles}.");
        }
    }
}