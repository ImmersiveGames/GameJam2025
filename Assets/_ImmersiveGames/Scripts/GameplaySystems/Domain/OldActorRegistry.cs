using System;
using System.Collections.Generic;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.NewScripts.Core.Logging;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    public interface IOldActorRegistry
    {
        event Action<IActor> ActorRegistered;
        event Action<IActor> ActorUnregistered;

        IReadOnlyCollection<IActor> Actors { get; }

        bool TryGetActor(string actorId, out IActor actor);

        bool Register(IActor actor);
        bool Unregister(IActor actor);
        bool UnregisterById(string actorId);
        void Clear();
    }
    
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class OldActorRegistry : IOldActorRegistry
    {
        private readonly Dictionary<string, IActor> _actorsById = new();

        public event Action<IActor> ActorRegistered;
        public event Action<IActor> ActorUnregistered;

        public IReadOnlyCollection<IActor> Actors => _actorsById.Values;

        public bool TryGetActor(string actorId, out IActor actor)
        {
            actor = null;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            return _actorsById.TryGetValue(actorId, out actor) && actor != null;
        }

        public bool Register(IActor actor)
        {
            if (actor == null)
            {
                return false;
            }

            // ActorId precisa ser não-vazio e estável (binder depende disso).
            if (string.IsNullOrWhiteSpace(actor.ActorId))
            {
                DebugUtility.LogWarning<OldActorRegistry>(
                    $"Tentativa de registrar ator sem ActorId. ActorName='{actor.ActorName}'.");
                return false;
            }

            if (_actorsById.TryGetValue(actor.ActorId, out var existing) && existing != null)
            {
                if (ReferenceEquals(existing, actor))
                {
                    // Idempotente.
                    return false;
                }

                DebugUtility.LogWarning<OldActorRegistry>(
                    $"ActorId duplicado detectado: '{actor.ActorId}'. " +
                    $"Existente='{existing.ActorName}', Novo='{actor.ActorName}'. Mantendo o existente.");
                return false;
            }

            _actorsById[actor.ActorId] = actor;
            ActorRegistered?.Invoke(actor);

            DebugUtility.LogVerbose<OldActorRegistry>(
                $"Ator registrado: {actor.ActorName} ({actor.ActorId}).");

            return true;
        }

        public bool Unregister(IActor actor)
        {
            if (actor == null)
            {
                return false;
            }

            return UnregisterById(actor.ActorId);
        }

        public bool UnregisterById(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            if (!_actorsById.TryGetValue(actorId, out var existing) || existing == null)
            {
                _actorsById.Remove(actorId);
                return false;
            }

            _actorsById.Remove(actorId);
            ActorUnregistered?.Invoke(existing);

            DebugUtility.LogVerbose<OldActorRegistry>(
                $"Ator removido do registry: {existing.ActorName} ({existing.ActorId}).");

            return true;
        }

        public void Clear()
        {
            if (_actorsById.Count == 0)
            {
                return;
            }

            var snapshot = new List<IActor>(_actorsById.Values);
            _actorsById.Clear();

            foreach (var a in snapshot)
            {
                if (a != null)
                {
                    ActorUnregistered?.Invoke(a);
                }
            }

            DebugUtility.LogVerbose<OldActorRegistry>("OldActorRegistry limpo.");
        }
    }
}

