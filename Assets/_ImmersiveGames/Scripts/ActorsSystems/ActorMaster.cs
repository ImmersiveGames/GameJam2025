using System;
using UnityEngine;
namespace _ImmersiveGames.Scripts.ActorsSystems
{
    public abstract class ActorMaster : MonoBehaviour
    {
        public event Action EventDeath;
        public virtual void OnEventDeath()
        {
            EventDeath?.Invoke();
        }
    }
}