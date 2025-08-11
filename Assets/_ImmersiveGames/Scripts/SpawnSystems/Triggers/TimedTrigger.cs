using _ImmersiveGames.Scripts.SpawnSystems.Data;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems.Triggers
{
    [DebugLevel(DebugLevel.Logs)]
    public abstract class TimedTrigger : BaseTrigger
    {
        protected float timer;

        protected TimedTrigger(EnhancedTriggerData data) : base(data)
        {
        }

        protected abstract bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);

        public override bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            if (!isActive)
            {
                triggerPosition = null;
                sourceObject = null;
                return false;
            }
            return OnCheckTrigger(out triggerPosition, out sourceObject);
        }
    }
}