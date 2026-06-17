using System;
using _ImmersiveGames.NewScripts.Actors.Foundation;

namespace _ImmersiveGames.NewScripts.Actors.ActivitySetup
{
    internal static class SceneAuthoredActorScopePolicy
    {
        internal static void ValidateOrThrow(
            ActorScope actorScope,
            string actorId,
            string sceneName)
        {
            string normalizedActorId = Normalize(actorId);
            string normalizedSceneName = Normalize(sceneName);

            switch (actorScope)
            {
                case ActorScope.ActivityScoped:
                case ActorScope.RouteScoped:
                    return;
                case ActorScope.Unknown:
                    throw new InvalidOperationException(
                        $"Scene-authored Actor requires explicit ActorScope in v0. actorId='{normalizedActorId}' scene='{normalizedSceneName}'.");
                case ActorScope.SessionScoped:
                    throw new InvalidOperationException(
                        $"Scene-authored Actor cannot use SessionScoped in v0. actorId='{normalizedActorId}' scene='{normalizedSceneName}'.");
                default:
                    throw new InvalidOperationException(
                        $"Scene-authored Actor uses unsupported ActorScope='{actorScope}' in v0. actorId='{normalizedActorId}' scene='{normalizedSceneName}'.");
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
