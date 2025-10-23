using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterMaster : ActorMaster, IEaterActor
    {
        [Header("Configuração")]
        [SerializeField] private EaterConfigSo config;

        public event Action<IDetectable> EventStartEatPlanet;
        public event Action<IDetectable> EventEndEatPlanet;
        public event Action<IDetectable, bool, IActor> EventConsumeResource;
        public event Action<IDetectable> EventEaterBite;
        public event Action<IActor> EventEaterTakeDamage;

        public EaterConfigSo Config => config;
        public EaterConfigSo GetConfig => config;

        public bool InHungry { get; internal set; }
        public bool IsEating { get; internal set; }

        public IActor EaterActor => this;

        public void OnEventStartEatPlanet(IDetectable detectable)
        {
            EventStartEatPlanet?.Invoke(detectable);
        }

        public void OnEventEndEatPlanet(IDetectable detectable)
        {
            EventEndEatPlanet?.Invoke(detectable);
        }

        public void OnEventConsumeResource(IDetectable detectable, bool desireSatisfied, IActor byActor)
        {
            EventConsumeResource?.Invoke(detectable, desireSatisfied, byActor);
        }

        public void OnEventEaterBite(IDetectable detectable)
        {
            EventEaterBite?.Invoke(detectable);
        }

        public void OnEventEaterTakeDamage(IActor byActor)
        {
            EventEaterTakeDamage?.Invoke(byActor);
        }
    }

    public interface IEaterActor
    {
        IActor EaterActor { get; }
    }
}