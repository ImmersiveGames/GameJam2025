using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class CircularZoomOutStrategy : ISpawnStrategy
    {
        private float _circleRadius; // Raio base do círculo na borda do planeta
        private readonly float _initialScale; // Escala inicial dos objetos
        private readonly float _animationDuration; // Duração da animação
        private readonly Ease _easeType; // Tipo de easing para a animação
        private readonly float _spiralRotations; // Número de rotações completas (em graus)
        private readonly float _spiralRadiusGrowth; // Taxa de crescimento do raio da espiral
        private readonly float _radiusVariation; // Variação aleatória no raio
        private readonly int _spawnCount; // Quantidade de objetos a spawnar

        private IObjectMovement _movement;
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

            _easeType = data.GetProperty("easeType", Ease.OutQuad);
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
                DebugUtility.LogWarning<CircularZoomOutStrategy>("PlanetsMaster não encontrado. Usando circleRadius padrão (5f).");
                _circleRadius = 5f;
            }

            int availableCount = pool.GetAvailableCount();
            DebugUtility.Log<CircularZoomOutStrategy>($"Pool tem {availableCount} objetos disponíveis, spawnCount configurado para {_spawnCount}.");

            for (int i = 0; i < _spawnCount; i++)
            {
                var obj = pool.GetObject(origin);
                if (obj == null)
                {
                    DebugUtility.LogWarning<CircularZoomOutStrategy>($"Falha ao obter objeto do pool na iteração {i}. Pool esgotado e expansão não permitida.");
                    break;
                }

                var go = obj.GetGameObject();
                go.transform.position = origin;
                go.transform.localScale = Vector3.one * _initialScale;

                float finalAngle = i * (360f / _spawnCount);
                float finalRadius = _circleRadius * _spiralRadiusGrowth * (1f + Random.Range(-_radiusVariation, _radiusVariation));
                var finalOffset = new Vector3(
                    Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(finalAngle * Mathf.Deg2Rad)
                ) * finalRadius;
                var targetPosition = origin + finalOffset;

                AnimateSpiral(go.transform, origin, finalAngle, finalRadius);

                obj.Activate(origin);
                if (sourceObject != null && sourceObject.TryGetComponent(out _movement))
                {
                    // Inicializar movimento se necessário
                }
                DebugUtility.Log<CircularZoomOutStrategy>(
                    $"Objeto '{go.name}' spawnado em {origin} movendo-se em espiral para {targetPosition}.",
                    "green",
                    go
                );
            }
        }

        private void AnimateSpiral(Transform target, Vector3 origin, float finalAngle, float finalRadius)
        {
            var sequence = DOTween.Sequence();

            float currentRadius = 0f;
            target.position = origin;

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
                    target.position = origin + offset;
                },
                finalRadius,
                _animationDuration
            ).SetEase(_easeType));

            sequence.Insert(0, target.DOScale(Vector3.one, _animationDuration).SetEase(_easeType));
        }
    }
}