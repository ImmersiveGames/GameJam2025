using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public abstract class TimedTrigger : BaseTrigger
    {
        protected float rearmDelay;
        protected float rearmTimer;
        protected bool isRearmed;

        protected TimedTrigger(EnhancedTriggerData data) : base(data)
        {
            rearmDelay = Mathf.Max(data.GetProperty("rearmDelay", 0.5f), 0f);
            rearmTimer = 0f;
            isRearmed = true;
        }

        public override bool CheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject)
        {
            triggerPosition = null;
            sourceObject = null;

            if (rearmTimer > 0f)
            {
                rearmTimer -= Time.deltaTime;
                if (rearmTimer <= 0f)
                    isRearmed = true;
            }

            if (!isActive || !isRearmed)
                return false;

            bool canTrigger = OnCheckTrigger(out triggerPosition, out sourceObject);
            if (canTrigger)
            {
                spawnCount++;
                if (maxSpawns >= 0 && spawnCount >= maxSpawns)
                {
                    SetActive(false);
                }
                else
                {
                    isRearmed = false;
                    rearmTimer = rearmDelay;
                }
                return true;
            }
            return false;
        }

        protected abstract bool OnCheckTrigger(out Vector3? triggerPosition, out GameObject sourceObject);

        public override void Reset()
        {
            base.Reset();
            isRearmed = true;
            rearmTimer = 0f;
        }
    }
}