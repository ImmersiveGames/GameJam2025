using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IEaterDomain
    {
        event Action<IActor> EaterRegistered;
        event Action<IActor> EaterUnregistered;

        IActor Eater { get; }

        bool TryGetSpawnTransform(out Vector3 position, out Quaternion rotation);

        bool RegisterEater(IActor actor);
        bool UnregisterEater(IActor actor);
        void Clear();
    }
    
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterDomain : IEaterDomain
    {
        public event Action<IActor> EaterRegistered;
        public event Action<IActor> EaterUnregistered;

        public IActor Eater { get; private set; }

        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _hasSpawnTransform;

        public bool RegisterEater(IActor actor)
        {
            if (actor == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(actor.ActorId))
            {
                DebugUtility.LogWarning<EaterDomain>(
                    $"Tentativa de registrar Eater sem ActorId. ActorName='{actor.ActorName}'.");
                return false;
            }

            if (Eater != null && !ReferenceEquals(Eater, actor))
            {
                DebugUtility.LogWarning<EaterDomain>(
                    $"Tentativa de registrar um segundo Eater. Atual='{Eater.ActorName}', Novo='{actor.ActorName}'. Ignorando novo.");
                return false;
            }

            if (Eater == null)
            {
                CaptureSpawnTransform(actor);
                Eater = actor;
                EaterRegistered?.Invoke(actor);

                DebugUtility.LogVerbose<EaterDomain>(
                    $"Eater registrado no domínio: {actor.ActorName} ({actor.ActorId}).");

                return true;
            }

            return false;
        }

        public bool UnregisterEater(IActor actor)
        {
            if (actor == null || Eater == null)
            {
                return false;
            }

            if (!ReferenceEquals(Eater, actor) && Eater.ActorId != actor.ActorId)
            {
                return false;
            }

            var old = Eater;
            Eater = null;

            _hasSpawnTransform = false;

            EaterUnregistered?.Invoke(old);

            DebugUtility.LogVerbose<EaterDomain>(
                $"Eater removido do domínio: {old.ActorName} ({old.ActorId}).");

            return true;
        }

        public void Clear()
        {
            if (Eater == null)
            {
                return;
            }

            var old = Eater;
            Eater = null;
            _hasSpawnTransform = false;
            EaterUnregistered?.Invoke(old);

            DebugUtility.LogVerbose<EaterDomain>("EaterDomain limpo.");
        }

        public bool TryGetSpawnTransform(out Vector3 position, out Quaternion rotation)
        {
            position = _spawnPosition;
            rotation = _spawnRotation;
            return _hasSpawnTransform;
        }

        private void CaptureSpawnTransform(IActor actor)
        {
            if (actor?.Transform == null)
            {
                DebugUtility.LogWarning<EaterDomain>(
                    "Tentativa de capturar SpawnTransform do Eater sem Transform disponível.");
                return;
            }

            _spawnPosition = actor.Transform.position;
            _spawnRotation = actor.Transform.rotation;
            _hasSpawnTransform = true;
        }
    }
}
