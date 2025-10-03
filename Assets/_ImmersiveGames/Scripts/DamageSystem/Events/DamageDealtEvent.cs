using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DamageSystem.Services;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.DamageSystem
{
    public class DamageDealtEvent : IEvent
    {
        public IActor SourceActor { get; }
        public IActor TargetActor { get; }
        public float DamageAmount { get; }
        public DamageType DamageType { get; }
        public Vector3 HitPosition { get; }

        public DamageDealtEvent(IActor sourceActor, IActor targetActor, float damageAmount, 
            DamageType damageType, Vector3 hitPosition)
        {
            SourceActor = sourceActor;
            TargetActor = targetActor;
            DamageAmount = damageAmount;
            DamageType = damageType;
            HitPosition = hitPosition;
        }
    }
}