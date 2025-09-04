using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.Utils.BusEventSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using _ImmersiveGames.Scripts.SpawnSystems.Events;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;

namespace _ImmersiveGames.Scripts.SpawnSystems.New
{
    public class OrbitSpawnStrategy : BaseSpawnStrategy
    {
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField, Min(0.1f)] private float initialRadius = 10f;
        [SerializeField, Min(0.1f)] private float spaceBetweenOrbits = 2f;
        [SerializeField] private CircleRotationMode rotationMode = CircleRotationMode.InheritFromSpawner;
        [SerializeField] private bool useRandomAngles = false;
        [SerializeField, Min(0f)] private float minAngleSeparationDegrees = 10f;
        [SerializeField, Min(0f)] private float angleVariationDegrees = 10f;
        [SerializeField, Range(1, 100)] private int maxAngleAttempts = 50;
        [SerializeField] private Transform centerTransform;
        [SerializeField, Min(0.1f)] private float defaultDiameter = 5f;

        private readonly List<float> _orbitalRadii = new();
        private readonly List<float> _diameters = new();
        private readonly List<float> _usedAngles = new();

        public override void Execute(ObjectPool pool, Transform spawnerTransform, bool exhaust, IActor actor = null, SpawnSystem spawnSystem = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<OrbitSpawnStrategy>("Pool is null.", this);
                return;
            }

            DebugUtility.LogVerbose<OrbitSpawnStrategy>(
                $"Executing spawn: spawnCount={spawnCount}, initialRadius={initialRadius}, " +
                $"spaceBetweenOrbits={spaceBetweenOrbits}, rotationMode={rotationMode}, " +
                $"useRandomAngles={useRandomAngles}, minAngleSeparationDegrees={minAngleSeparationDegrees}, " +
                $"angleVariationDegrees={angleVariationDegrees}, centerTransform={(centerTransform != null ? centerTransform.name : "null")}, " +
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

            _orbitalRadii.Clear();
            _diameters.Clear();
            _usedAngles.Clear();

            // Obtém diâmetros configurados pelos masters ou usa defaultDiameter
            for (int i = 0; i < successfulSpawns; i++)
            {
                var poolableObject = poolables[i];
                float diameter = defaultDiameter;
                var sizeMeasurable = poolableObject.GetGameObject().GetComponent<ISizeMeasurable>();
                if (sizeMeasurable != null)
                {
                    diameter = sizeMeasurable.GetDiameter();
                    if (diameter <= 0f)
                    {
                        DebugUtility.LogWarning<OrbitSpawnStrategy>(
                            $"Object '{poolableObject.GetGameObject().name}' returned invalid diameter {diameter:F2}. Using default: {defaultDiameter}.",
                            this);
                        diameter = defaultDiameter;
                    }
                }
                else
                {
                    DebugUtility.LogVerbose<OrbitSpawnStrategy>(
                        $"Object '{poolableObject.GetGameObject().name}' does not implement ISizeMeasurable. Using default diameter: {defaultDiameter}.",
                        "blue");
                }

                _diameters.Add(diameter);
                DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Object {i} diameter: {diameter:F2}", "cyan", this);
            }

            // Posiciona e ativa objetos
            for (int i = 0; i < successfulSpawns; i++)
            {
                float prevRadius = i > 0 ? _orbitalRadii[i - 1] : 0f;
                float prevDiameter = i > 0 ? _diameters[i - 1] : 0f;
                float currentRadius = i == 0 ? initialRadius : prevRadius + (prevDiameter / 2) + spaceBetweenOrbits + (_diameters[i] / 2);

                float angleRad = useRandomAngles
                    ? GetRandomAngleWithValidation(successfulSpawns)
                    : GetOptimalAngle(i, successfulSpawns);

                Vector3 position = center + new Vector3(
                    Mathf.Cos(angleRad) * currentRadius,
                    0f,
                    Mathf.Sin(angleRad) * currentRadius
                );

                position = ValidatePosition(position, currentRadius, i, poolables, center, _diameters[i]);

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
                _orbitalRadii.Add(currentRadius);
            }

            if (spawnSystem != null)
            {
                OrbitFilteredEventBus.RaiseFiltered(new OrbitsSpawnedEvent(poolables, center, _orbitalRadii, spawnSystem));
            }
            else
            {
                DebugUtility.LogWarning<OrbitSpawnStrategy>("SpawnSystem is null. Skipping OrbitsSpawnedEvent.", this);
            }

            DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Completed spawn: {successfulSpawns}/{spawnCount} objects spawned.", "cyan", this);
            LogExhausted(pool, successfulSpawns, spawnCount);
        }

        private float GetOptimalAngle(int index, int totalObjects)
        {
            float baseAngle = (360f / totalObjects) * index;
            if (angleVariationDegrees > 0f)
            {
                baseAngle += Random.Range(-angleVariationDegrees, angleVariationDegrees);
            }
            baseAngle = NormalizeAngle(baseAngle);
            DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Optimal angle: {baseAngle:F1}° for object {index}/{totalObjects}", "blue", this);
            return baseAngle * Mathf.Deg2Rad;
        }

        private float GetRandomAngleWithValidation(int totalObjects)
        {
            if (_usedAngles.Count == 0)
            {
                float firstAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                _usedAngles.Add(firstAngle);
                DebugUtility.LogVerbose<OrbitSpawnStrategy>($"First random angle: {firstAngle * Mathf.Rad2Deg:F1}°", "blue", this);
                return firstAngle;
            }

            float minAngleSeparation = Mathf.Max(360f / (totalObjects * 1.5f), minAngleSeparationDegrees);
            for (int attempts = 0; attempts < maxAngleAttempts; attempts++)
            {
                float candidateAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                if (IsAngleValid(candidateAngle, minAngleSeparation))
                {
                    _usedAngles.Add(candidateAngle);
                    DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Valid random angle: {candidateAngle * Mathf.Rad2Deg:F1}° (attempt {attempts + 1})", "blue", this);
                    return candidateAngle;
                }
            }

            float fallbackAngle = (360f / totalObjects) * _usedAngles.Count * Mathf.Deg2Rad;
            _usedAngles.Add(fallbackAngle);
            DebugUtility.LogWarning<OrbitSpawnStrategy>($"Fallback angle: {fallbackAngle * Mathf.Rad2Deg:F1}° after {maxAngleAttempts} attempts", this);
            return fallbackAngle;
        }

        private bool IsAngleValid(float candidateAngle, float minSeparation)
        {
            float candidateAngleDegrees = candidateAngle * Mathf.Rad2Deg;
            return _usedAngles.All(usedAngle => Mathf.Abs(Mathf.DeltaAngle(candidateAngleDegrees, usedAngle * Mathf.Rad2Deg)) >= minSeparation);
        }

        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            return angle < 0f ? angle + 360f : angle;
        }

        private Vector3 ValidatePosition(Vector3 position, float radius, int index, List<IPoolable> poolables, Vector3 center, float currentDiameter)
        {
            Vector3 adjustedPosition = position;
            float adjustedRadius = radius;
            int attempts = 0;
            const int maxAttempts = 3;

            while (attempts < maxAttempts)
            {
                bool hasCollision = false;
                for (int j = 0; j < index; j++)
                {
                    float prevDiameter = _diameters[j];
                    float distance = Vector3.Distance(adjustedPosition, poolables[j].GetGameObject().transform.position);
                    if (distance < (prevDiameter / 2) + spaceBetweenOrbits + (currentDiameter / 2))
                    {
                        hasCollision = true;
                        adjustedRadius += spaceBetweenOrbits * 0.5f;
                        float angleRad = Mathf.Atan2(adjustedPosition.z - center.z, adjustedPosition.x - center.x);
                        adjustedPosition = center + new Vector3(
                            Mathf.Cos(angleRad) * adjustedRadius,
                            0f,
                            Mathf.Sin(angleRad) * adjustedRadius
                        );
                        attempts++;
                        DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Collision detected for object {index} with {j}. New radius: {adjustedRadius:F2}", "yellow", this);
                        break;
                    }
                }
                if (!hasCollision) break;
            }

            if (attempts >= maxAttempts)
            {
                DebugUtility.LogWarning<OrbitSpawnStrategy>($"Could not avoid collision for object {index} after {attempts} attempts", this);
            }

            return adjustedPosition;
        }

        public override void SetCenterTransform(Transform newCenter)
        {
            centerTransform = newCenter;
            DebugUtility.LogVerbose<OrbitSpawnStrategy>($"Center transform set to {(newCenter != null ? newCenter.name : "null")}.", "cyan", this);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = (centerTransform != null) ? centerTransform.position : transform.position;
            center.y = 0f;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _orbitalRadii.Count; i++)
            {
                Gizmos.DrawWireSphere(center, _orbitalRadii[i]);
            }
        }
    }
}