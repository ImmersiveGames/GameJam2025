using System.Collections.Generic;
using _ImmersiveGames.Scripts.SpawnSystems.Interfaces;
using UnityEngine;
namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public enum SpawnDisposition
    {
        None, // Todos na mesma posição
        Line, // Em linha com direção configurável
        Arc   // Em arco (círculo ou leque)
    }

    [CreateAssetMenu(fileName = "SpawnStrategyConfig", menuName = "ImmersiveGames/SpawnStrategies/SpawnStrategyConfig", order = 1)]
    public class SpawnStrategyConfig : ScriptableObject
    {
        [SerializeField] private string strategyName = "Spawn Strategy";
        [SerializeField] private Vector3 defaultPosition = Vector3.zero; // Posição inicial padrão
        [SerializeField] private Vector3 direction = Vector3.forward; // Direção para Line e Arc
        [SerializeField] private bool rotateToDirection = false; // Rotacionar objetos para a direção
        [SerializeField] private SpawnDisposition disposition = SpawnDisposition.None;
        [SerializeField] private float delayBetweenSpawns = 0f; // Atraso entre spawns (0 = imediato)
        [Header("Line Disposition Settings")]
        [SerializeField] private float lineDistance = 1f; // Distância entre objetos
        [Header("Arc Disposition Settings")]
        [SerializeField] private float arcRadius = 1f; // Raio do arco/círculo
        [SerializeField] private float arcAngle = 360f; // Ângulo do arco (360 = círculo, menor = leque)

        public ISpawnStrategyInstance CreateInstance()
        {
            return new SpawnStrategyInstance(
                strategyName,
                defaultPosition,
                direction.normalized,
                rotateToDirection,
                disposition,
                delayBetweenSpawns,
                lineDistance,
                arcRadius,
                arcAngle
            );
        }
    }

    public class SpawnStrategyInstance : ISpawnStrategyInstance
    {
        private readonly string _strategyName;
        private Vector3 _position;
        private Transform _target;
        private readonly Vector3 _direction;
        private readonly bool _rotateToDirection;
        private readonly SpawnDisposition _disposition;
        private readonly float _delayBetweenSpawns;
        private readonly float _lineDistance;
        private readonly float _arcRadius;
        private readonly float _arcAngle;

        public SpawnStrategyInstance(
            string strategyName,
            Vector3 defaultPosition,
            Vector3 direction,
            bool rotateToDirection,
            SpawnDisposition disposition,
            float delayBetweenSpawns,
            float lineDistance,
            float arcRadius,
            float arcAngle)
        {
            _strategyName = strategyName;
            _position = defaultPosition;
            _direction = direction;
            _rotateToDirection = rotateToDirection;
            _disposition = disposition;
            _delayBetweenSpawns = delayBetweenSpawns;
            _lineDistance = lineDistance;
            _arcRadius = arcRadius;
            _arcAngle = arcAngle;
        }

        public string StrategyName => _strategyName;

        public void SetTarget(Transform target)
        {
            _target = target;
        }

        public void SetPosition(Vector3 position)
        {
            _target = null; // Desativar alvo dinâmico
            _position = position;
        }

        public IEnumerable<(Vector3 position, Quaternion rotation)> GetSpawnPositions(int count)
        {
            Vector3 spawnPosition = _target != null ? _target.position : _position;

            if (count > 1 && _disposition == SpawnDisposition.None)
            {
                Debug.LogWarning($"SpawnStrategyInstance: disposition 'None' with count {count} may result in overlapping objects.");
            }

            switch (_disposition)
            {
                case SpawnDisposition.None:
                    Quaternion noneRotation = _rotateToDirection ? Quaternion.LookRotation(_direction, Vector3.up) : Quaternion.identity;
                    for (int i = 0; i < count; i++)
                    {
                        yield return (spawnPosition, noneRotation);
                    }
                    break;

                case SpawnDisposition.Line:
                    Quaternion lineRotation = _rotateToDirection ? Quaternion.LookRotation(_direction, Vector3.up) : Quaternion.identity;
                    for (int i = 0; i < count; i++)
                    {
                        Vector3 offset = _direction * (_lineDistance * (i - (count - 1) / 2f));
                        yield return (spawnPosition + offset, lineRotation);
                    }
                    break;

                case SpawnDisposition.Arc:
                    for (int i = 0; i < count; i++)
                    {
                        float t = count > 1 ? (float)i / (count - 1) : 0f;
                        float angle = Mathf.Lerp(-_arcAngle / 2, _arcAngle / 2, t);
                        Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                        Vector3 offset = rotation * (_direction * _arcRadius);
                        Quaternion objectRotation = _rotateToDirection ? rotation * Quaternion.LookRotation(_direction, Vector3.up) : Quaternion.identity;
                        yield return (spawnPosition + offset, objectRotation);
                    }
                    break;
            }
        }

        public float GetSpawnDelay(int index)
        {
            return index * _delayBetweenSpawns;
        }
    }
}