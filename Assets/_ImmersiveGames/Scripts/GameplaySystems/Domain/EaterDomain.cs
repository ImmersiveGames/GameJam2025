using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.GameplaySystems.Domain
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterDomain : IEaterDomain
    {
        public event Action<IActor> EaterRegistered;
        public event Action<IActor> EaterUnregistered;

        public IActor Eater { get; private set; }

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
            EaterUnregistered?.Invoke(old);

            DebugUtility.LogVerbose<EaterDomain>("EaterDomain limpo.");
        }
    }
}
