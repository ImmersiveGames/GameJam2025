using System;
using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Infrastructure.DebugLog;

namespace _ImmersiveGames.NewScripts.Infrastructure.Actors
{
    /// <summary>
    /// Registro básico de atores para o escopo da cena.
    /// Responsável por garantir unicidade de IDs e facilitar consulta/limpeza.
    /// </summary>
    public interface IActorRegistry
    {
        IReadOnlyCollection<IActor> Actors { get; }

        int Count { get; }

        bool TryGetActor(string actorId, out IActor actor);

        bool Register(IActor actor);

        bool Unregister(string actorId);

        void Clear();

        void GetActors(List<IActor> target);
    }

    public sealed class ActorRegistry : IActorRegistry
    {
        private readonly Dictionary<string, IActor> _actors = new(StringComparer.Ordinal);

        public IReadOnlyCollection<IActor> Actors => _actors.Values;

        public int Count => _actors.Count;

        public bool TryGetActor(string actorId, out IActor actor)
        {
            actor = null;
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            return _actors.TryGetValue(actorId, out actor) && actor != null;
        }

        public bool Register(IActor actor)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.ActorId))
            {
                return false;
            }

            if (_actors.TryGetValue(actor.ActorId, out var existing) && existing != null)
            {
                if (ReferenceEquals(existing, actor))
                {
                    return false;
                }

                DebugUtility.LogWarning(typeof(ActorRegistry),
                    $"ActorId duplicado detectado: {actor.ActorId}.");
                return false;
            }

            _actors[actor.ActorId] = actor;
            DebugUtility.LogVerbose(typeof(ActorRegistry), $"Ator registrado: {actor.ActorId}.");
            return true;
        }

        public bool Unregister(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            if (!_actors.Remove(actorId, out var actor))
            {
                return false;
            }

            if (actor != null)
            {
                DebugUtility.LogVerbose(typeof(ActorRegistry), $"Ator removido: {actorId}.");
            }

            return true;
        }

        public void Clear()
        {
            int removedCount = _actors.Count;
            _actors.Clear();
            DebugUtility.LogVerbose(typeof(ActorRegistry), $"Registry limpo. Removidos: {removedCount}.");
        }

        public void GetActors(List<IActor> target)
        {
            if (target == null)
            {
                return;
            }

            target.Clear();

            if (_actors.Count == 0)
            {
                return;
            }

            var orderedActors = new List<IActor>(_actors.Count);
            foreach (var actor in _actors.Values)
            {
                if (actor != null)
                {
                    orderedActors.Add(actor);
                }
            }

            orderedActors.Sort((left, right) =>
                string.CompareOrdinal(left?.ActorId, right?.ActorId));

            target.AddRange(orderedActors);
        }
    }
}
