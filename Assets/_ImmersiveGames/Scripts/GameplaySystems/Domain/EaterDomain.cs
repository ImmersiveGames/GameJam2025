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

        bool TryGetSpawnPose(out Pose pose);

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

        private Pose _eaterSpawnPose;
        private bool _spawnPoseCaptured;

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
                CaptureSpawnPose(actor);
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

            _spawnPoseCaptured = false;

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
            _spawnPoseCaptured = false;
            EaterUnregistered?.Invoke(old);

            DebugUtility.LogVerbose<EaterDomain>("EaterDomain limpo.");
        }

        public bool TryGetSpawnPose(out Pose pose)
        {
            pose = _eaterSpawnPose;
            return _spawnPoseCaptured;
        }

        private void CaptureSpawnPose(IActor actor)
        {
            if (actor?.Transform == null)
            {
                DebugUtility.LogWarning<EaterDomain>(
                    "Tentativa de capturar SpawnPose do Eater sem Transform disponível.");
                return;
            }

            _eaterSpawnPose = new Pose(actor.Transform.position, actor.Transform.rotation);
            _spawnPoseCaptured = true;
        }
    }
}
