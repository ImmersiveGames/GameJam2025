using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems.Interfaces;
using DG.Tweening;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems.Strategies
{
    [DebugLevel(DebugLevel.Warning)]
    public class CircularZoomOutStrategy : ISpawnStrategy
    {
        private float _circleRadius;
        private readonly float _initialScale;
        private readonly float _animationDuration;
        private readonly Ease _easeType;
        private readonly float _spiralRotations;
        private readonly float _spiralRadiusGrowth;
        private readonly float _radiusVariation;
        private readonly int _spawnCount;
        private PlanetsMaster _planetsMaster;

        public CircularZoomOutStrategy(EnhancedStrategyData data)
        {
            _initialScale = data.GetProperty("initialScale", 0.1f);
            if (_initialScale <= 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("initialScale deve ser maior que 0. Usando 0.1.");
                _initialScale = 0.1f;
            }

            _animationDuration = data.GetProperty("animationDuration", 1f);
            if (_animationDuration <= 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("animationDuration deve ser maior que 0. Usando 1.");
                _animationDuration = 1f;
            }

            _spiralRotations = data.GetProperty("spiralRotations", 360f);
            if (_spiralRotations < 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("spiralRotations não pode ser negativo. Usando 360.");
                _spiralRotations = 360f;
            }

            _spiralRadiusGrowth = data.GetProperty("spiralRadiusGrowth", 1f);
            if (_spiralRadiusGrowth <= 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("spiralRadiusGrowth deve ser maior que 0. Usando 1.");
                _spiralRadiusGrowth = 1f;
            }

            _radiusVariation = data.GetProperty("radiusVariation", 0f);
            if (_radiusVariation < 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("radiusVariation não pode ser negativo. Usando 0.");
                _radiusVariation = 0f;
            }

            _spawnCount = data.GetProperty("spawnCount", 5);
            if (_spawnCount <= 0)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("spawnCount deve ser maior que 0. Usando 5.");
                _spawnCount = 5;
            }
        }

        public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("ObjectPool é nulo.");
                return;
            }

            if (sourceObject != null && sourceObject.TryGetComponent(out _planetsMaster))
            {
                _circleRadius = _planetsMaster.GetPlanetInfo().planetDiameter * 1.2f;
            }
            else
            {
                DebugUtility.LogWarning<CircularZoomOutStrategy>("PlanetsMaster não encontrado. Usando circleRadius padrão (5f).", sourceObject);
                _circleRadius = 5f;
            }

            Transform target = GetDetectorFromSource(sourceObject);
            var owner = sourceObject ? sourceObject.GetComponent<IActor>() : null;
            for (int i = 0; i < _spawnCount; i++)
            {
                SpawnSingleObject(pool, origin, target, i, owner);
            }

            DebugUtility.Log<CircularZoomOutStrategy>($"Spawn concluído: {_spawnCount} objetos instanciados em volta de {(sourceObject != null ? sourceObject.name : "origem desconhecida")}.", "green");
        }


        private Transform GetDetectorFromSource(GameObject sourceObject)
        {
            if (sourceObject == null || !sourceObject.TryGetComponent(out PlanetsMaster planetsMaster))
            {
                DebugUtility.LogWarning<CircularZoomOutStrategy>("sourceObject é nulo ou não possui PlanetsMaster.");
                return null;
            }

            var detectors = planetsMaster.GetDetectors();
            if (detectors == null || detectors.Count == 0) return null;

            IDetector closestDetector = null;
            float minDistance = float.MaxValue;
            Vector3 planetPosition = sourceObject.transform.position;

            foreach (var detector in detectors)
            {
                if (detector?.Owner == null) continue;
                float distance = Vector3.Distance(planetPosition, detector.Owner.Transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestDetector = detector;
                }
            }

            return closestDetector?.Owner?.Transform;
        }

        private void SpawnSingleObject(ObjectPool pool, Vector3 origin, Transform target, int iteration, IActor owner)
        {
            var obj = pool.GetObject(origin);
            if (obj == null)
            {
                DebugUtility.LogWarning<CircularZoomOutStrategy>($"Falha ao obter objeto do pool na iteração {iteration}. Pool esgotado.");
                return;
            }

            var go = obj.GetGameObject();
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * _initialScale;

            float finalAngle = iteration * (360f / _spawnCount);
            float finalRadius = _circleRadius * _spiralRadiusGrowth * (1f + Random.Range(-_radiusVariation, _radiusVariation));
            var finalOffset = new Vector3(
                Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                0,
                Mathf.Sin(finalAngle * Mathf.Deg2Rad)
            ) * finalRadius;
            var targetPosition = origin + finalOffset;

            SetupObjectMovement(obj, target, targetPosition);
            obj.Activate(targetPosition, owner);
        }


        private void SetupObjectMovement(IPoolable obj, Transform target, Vector3 targetPosition)
        {
            var go = obj.GetGameObject();
            if (!go.TryGetComponent(out IMoveObject movement))
            {
                AnimateSpiral(go.transform, go.transform.position, targetPosition);
                return;
            }

            AnimateSpiral(go.transform, go.transform.position, targetPosition, () =>
            {
                if (movement != null && go != null && go.activeInHierarchy)
                {
                    movement.Initialize(go.transform.position, 0f, target);
                }
            });
        }


        private void AnimateSpiral(Transform targetTransform, Vector3 origin, Vector3 targetPosition, System.Action onComplete = null)
        {
            var sequence = DOTween.Sequence();
            float currentRadius = 0f;
            float finalRadius = Vector3.Distance(origin, targetPosition);
            float finalAngle = Mathf.Atan2(targetPosition.z - origin.z, targetPosition.x - origin.x) * Mathf.Rad2Deg;

            targetTransform.position = origin;

            sequence.Append(DOTween.To(
                () => currentRadius,
                radius =>
                {
                    currentRadius = radius;
                    float t = radius / finalRadius;
                    float currentAngle = t * _spiralRotations;
                    var offset = new Vector3(
                        Mathf.Cos((finalAngle + currentAngle) * Mathf.Deg2Rad),
                        0,
                        Mathf.Sin((finalAngle + currentAngle) * Mathf.Deg2Rad)
                    ) * (currentRadius * _spiralRadiusGrowth);
                    targetTransform.position = origin + offset;
                },
                finalRadius,
                _animationDuration
            ).SetEase(_easeType));

            sequence.Insert(0, targetTransform.DOScale(Vector3.one, _animationDuration).SetEase(_easeType));

            if (onComplete != null)
            {
                sequence.OnComplete(onComplete.Invoke);
            }
        }
    }
}