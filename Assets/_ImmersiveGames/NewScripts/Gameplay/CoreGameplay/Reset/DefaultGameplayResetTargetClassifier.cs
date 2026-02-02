using System.Collections.Generic;
using _ImmersiveGames.NewScripts.Core.Logging;
using _ImmersiveGames.NewScripts.Runtime.Actors;
using _ImmersiveGames.NewScripts.Runtime.Mode;
namespace _ImmersiveGames.NewScripts.Gameplay.CoreGameplay.Reset
{
    /// <summary>
    /// Classificador padrão de alvos de reset.
    /// - PlayersOnly: alias para ActorKind.Player.
    /// - EaterOnly: procura por componente chamado "EaterActor" (string-based), para não acoplar compile-time.
    /// - ActorIdSet: usa ActorRegistry.TryGetActor para os ids do request.
    /// - AllActorsInScene: usa todos do registry.
    /// - ByActorKind: filtra via IActorKindProvider.
    /// </summary>
    public sealed class DefaultGameplayResetTargetClassifier : IGameplayResetTargetClassifier
    {
        private readonly IRuntimeModeProvider _runtimeModeProvider;
        private readonly IDegradedModeReporter _degradedModeReporter;

        /// <summary>
        /// Construtor padrão (compatibilidade). Não depende de modo em runtime.
        /// </summary>
        public DefaultGameplayResetTargetClassifier() : this(null, null)
        {
        }

        /// <summary>
        /// Construtor para cenários onde o bootstrap quer injetar modo/degraded.
        /// (O classificador atual não precisa do modo para funcionar, mas mantemos a assinatura para integração.)
        /// </summary>
        public DefaultGameplayResetTargetClassifier(IRuntimeModeProvider runtimeModeProvider, IDegradedModeReporter degradedModeReporter)
        {
            _runtimeModeProvider = runtimeModeProvider;
            _degradedModeReporter = degradedModeReporter;
        }

        public void CollectTargets(GameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            results.Clear();

            if (actorRegistry == null)
            {
                return;
            }

            switch (request.Target)
            {
                case GameplayResetTarget.AllActorsInScene:
                    AddAllActors(actorRegistry, results);
                    return;

                case GameplayResetTarget.ActorIdSet:
                    AddByActorIdSet(request, actorRegistry, results);
                    return;

                case GameplayResetTarget.PlayersOnly:
                    AddByActorKind(actorRegistry, results, ActorKind.Player);
                    return;

                case GameplayResetTarget.ByActorKind:
                    AddByActorKind(actorRegistry, results, request.ActorKind);
                    return;

                case GameplayResetTarget.EaterOnly:
                    AddEatersKindFirstWithFallback(actorRegistry, results, out bool fallbackUsed);
                    if (fallbackUsed)
                    {
                        DebugUtility.LogWarning(typeof(DefaultGameplayResetTargetClassifier),
                            "GameplayResetTarget.EaterOnly using string-based fallback (EaterActor).");
                    }
                    return;

                default:
                    AddAllActors(actorRegistry, results);
                    return;
            }
        }

        private static void AddAllActors(IActorRegistry actorRegistry, List<IActor> results)
        {
            if (actorRegistry.Actors == null)
            {
                return;
            }

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor != null)
                {
                    results.Add(actor);
                }
            }
        }

        private static void AddByActorIdSet(GameplayResetRequest request, IActorRegistry actorRegistry, List<IActor> results)
        {
            IReadOnlyList<string> ids = request.ActorIds;
            if (ids == null || ids.Count == 0)
            {
                return;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                string id = ids[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                if (actorRegistry.TryGetActor(id, out var actor) && actor != null)
                {
                    results.Add(actor);
                }
            }
        }

        private static void AddByActorKind(IActorRegistry actorRegistry, List<IActor> results, ActorKind kind)
        {
            if (actorRegistry.Actors == null)
            {
                return;
            }

            if (kind == ActorKind.Unknown)
            {
                return;
            }

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                if (GameplayResetTargetMatching.MatchesActorKind(actor, kind))
                {
                    results.Add(actor);
                }
            }
        }

        private static void AddEatersKindFirstWithFallback(
            IActorRegistry actorRegistry,
            List<IActor> results,
            out bool fallbackUsed)
        {
            fallbackUsed = false;

            if (actorRegistry.Actors == null)
            {
                return;
            }

            foreach (var actor in actorRegistry.Actors)
            {
                if (actor == null)
                {
                    continue;
                }

                if (GameplayResetTargetMatching.MatchesEaterKindFirstWithFallback(actor, out bool usedFallback))
                {
                    results.Add(actor);
                    if (usedFallback)
                    {
                        fallbackUsed = true;
                    }
                }
            }
        }
    }
}
