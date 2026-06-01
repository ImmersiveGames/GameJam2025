using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Actors.Presentation.Contracts;
using _ImmersiveGames.NewScripts.Actors.Presentation.Runtime;
using _ImmersiveGames.NewScripts.Actors.Runtime;
using _ImmersiveGames.NewScripts.SessionActivity.Contracts;
using UnityEngine;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    /// <summary>
    /// Registry técnico neutro para actors descobertos em cena.
    /// Encapsula o registry legado atual para que pipelines não dependam de shape NonPlayer.
    /// </summary>
    public sealed class ActivitySceneActorRegistry
    {
        private readonly ActivityNonPlayerActorRegistry _legacySceneActorRegistry = new();

        public void BeginActivityScope(SessionActivityIdentity identity)
        {
            _legacySceneActorRegistry.BeginActivityScope(identity);
        }

        public void ClearAllRouteRetained()
        {
            _legacySceneActorRegistry.ClearAllRouteRetained();
        }

        public void RegisterDiscovered(
            NonPlayerActorIdentityRecord identity,
            NonPlayerActor actor,
            GameObject actorInstance)
        {
            _legacySceneActorRegistry.RegisterDiscovered(identity, actor, actorInstance);
        }

        public IReadOnlyList<NonPlayerActorRuntimeEntry> GetActiveEntries(SessionActivityIdentity identity)
        {
            return _legacySceneActorRegistry.GetActiveEntries(identity);
        }

        public bool TryGetActive(
            SessionActivityIdentity identity,
            string actorId,
            out NonPlayerActorRuntimeEntry entry)
        {
            return _legacySceneActorRegistry.TryGetActive(identity, actorId, out entry);
        }

        public void SetPresentationHandle(
            SessionActivityIdentity identity,
            string actorId,
            ActorPresentationRuntimeHandle handle)
        {
            _legacySceneActorRegistry.SetPresentationHandle(identity, actorId, handle);
        }

        public void ClearPresentationHandle(SessionActivityIdentity identity, string actorId)
        {
            _legacySceneActorRegistry.ClearPresentationHandle(identity, actorId);
        }
    }
}
