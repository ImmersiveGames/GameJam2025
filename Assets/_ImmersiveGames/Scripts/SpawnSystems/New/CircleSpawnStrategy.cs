using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class CircleSpawnStrategy : BaseSpawnStrategy
    {
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField, Min(0.1f)] private float radius = 1f;
        [SerializeField] private CircleRotationMode rotationMode = CircleRotationMode.InheritFromSpawner;
        [SerializeField, Range(0f, 360f)] private float startAngle = 0f;
        [SerializeField, Range(0f, 360f)] private float endAngle = 360f;
        [SerializeField] private Transform centerTransform;

        public override void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<CircleSpawnStrategy>("Pool is null.", this);
                return;
            }

            DebugUtility.LogVerbose<CircleSpawnStrategy>(
                $"Executing spawn: spawnCount={spawnCount}, radius={radius}, rotationMode={rotationMode}, " +
                $"startAngle={startAngle}, endAngle={endAngle}, centerTransform={(centerTransform != null ? centerTransform.name : "null")}, " +
                $"actor={(actor != null ? actor.Name : "null")}, spawnSystem={(spawnSystem != null ? spawnSystem.name : "null")}, exhaust={exhaust}",
                "cyan", this);

            Vector3 center = (centerTransform != null) ? centerTransform.position : spawnerTransform.position;
            center.y = 0f;
            Quaternion spawnerRotation = spawnerTransform.rotation;

            var poolables = GetObjects(pool, spawnCount, actor, exhaust);
            int successfulSpawns = poolables.Count;

            if (successfulSpawns == 0)
            {
                return;
            }

            float angleRange = Mathf.Clamp(endAngle - startAngle, 0f, 360f);
            for (int i = 0; i < successfulSpawns; i++)
            {
                float angleDeg = startAngle + (successfulSpawns > 1 ? i * (angleRange / (successfulSpawns - 1)) : 0f);
                float angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 position = center + new Vector3(
                    Mathf.Cos(angleRad) * radius,
                    0f,
                    Mathf.Sin(angleRad) * radius
                );

                Quaternion objectRotation;
                switch (rotationMode)
                {
                    case CircleRotationMode.LookToCenter:
                        Vector3 directionToCenter = center - position;
                        objectRotation = Quaternion.LookRotation(directionToCenter.normalized, Vector3.up);
                        break;
                    case CircleRotationMode.LookAwayFromCenter:
                        Vector3 directionAwayFromCenter = position - center;
                        objectRotation = Quaternion.LookRotation(directionAwayFromCenter.normalized, Vector3.up);
                        break;
                    case CircleRotationMode.InheritFromSpawner:
                    default:
                        objectRotation = spawnerRotation;
                        break;
                }

                ActivatePoolable(poolables[i], position, objectRotation, actor, pool);
            }

            DebugUtility.LogVerbose<CircleSpawnStrategy>($"Completed spawn: {successfulSpawns}/{spawnCount} objects spawned.", "cyan", this);
            LogExhausted(pool, successfulSpawns, spawnCount);
        }

        public override void SetCenterTransform(Transform newCenter)
        {
            centerTransform = newCenter;
            DebugUtility.LogVerbose<CircleSpawnStrategy>($"Center transform set to {(newCenter != null ? newCenter.name : "null")}.", "cyan", this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = (centerTransform != null) ? centerTransform.position : transform.position;
            center.y = 0f;
            Gizmos.color = Color.blue;
            float startRad = startAngle * Mathf.Deg2Rad;
            float endRad = endAngle * Mathf.Deg2Rad;
            Vector3 startPos = center + new Vector3(Mathf.Cos(startRad) * radius, 0f, Mathf.Sin(startRad) * radius);
            Vector3 endPos = center + new Vector3(Mathf.Cos(endRad) * radius, 0f, Mathf.Sin(endRad) * radius);
            Gizmos.DrawLine(center, startPos);
            Gizmos.DrawLine(center, endPos);
            for (int i = 0; i < 20; i++)
            {
                float angle1 = startAngle + (endAngle - startAngle) * (i / 20f);
                float angle2 = startAngle + (endAngle - startAngle) * ((i + 1) / 20f);
                Vector3 pos1 = center + new Vector3(Mathf.Cos(angle1 * Mathf.Deg2Rad) * radius, 0f, Mathf.Sin(angle1 * Mathf.Deg2Rad) * radius);
                Vector3 pos2 = center + new Vector3(Mathf.Cos(angle2 * Mathf.Deg2Rad) * radius, 0f, Mathf.Sin(angle2 * Mathf.Deg2Rad) * radius);
                Gizmos.DrawLine(pos1, pos2);
            }
        }
    }
}