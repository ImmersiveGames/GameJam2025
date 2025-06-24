using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.SpawnSystems
{
    public class CircularZoomOutStrategy : ISpawnStrategy
    {
        private readonly float _circleRadius; // Raio base do círculo na borda do planeta
        private readonly float _initialScale; // Escala inicial dos objetos
        private readonly float _animationDuration; // Duração da animação
        private readonly Ease _easeType; // Tipo de easing para a animação
        private readonly float _spiralRotations; // Número de rotações completas (em graus)
        private readonly float _spiralRadiusGrowth; // Taxa de crescimento do raio da espiral
        private readonly float _radiusVariation; // Variação aleatória no raio final

        public CircularZoomOutStrategy(EnhancedStrategyData data)
        {
            _circleRadius = data.GetProperty("circleRadius", 5f);
            if (_circleRadius <= 0f)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("circleRadius deve ser maior que 0. Usando 5.");
                _circleRadius = 5f;
            }

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

            _easeType = data.GetProperty("easeType", Ease.OutQuad);
        }

        public void Spawn(ObjectPool pool, Vector3 origin, GameObject sourceObject = null)
        {
            if (pool == null)
            {
                DebugUtility.LogError<CircularZoomOutStrategy>("ObjectPool é nulo.");
                return;
            }

            int count = pool.GetAvailableCount();
            if (count == 0)
            {
                DebugUtility.LogWarning<CircularZoomOutStrategy>("Nenhum objeto disponível no pool.");
                return;
            }

            // Spawnar todos os objetos disponíveis no centro (origin)
            for (int i = 0; i < count; i++)
            {
                var obj = pool.GetObject(origin);
                if (obj == null)
                {
                    DebugUtility.LogWarning<CircularZoomOutStrategy>($"Falha ao obter objeto do pool na iteração {i}.");
                    continue;
                }

                // Configura a escala inicial e posição
                GameObject go = obj.GetGameObject();
                go.transform.position = origin;
                go.transform.localScale = Vector3.one * _initialScale;

                // Calcula a posição final na borda do planeta (distribuição circular)
                float finalAngle = i * (360f / count); // Ângulo final em graus
                // Aplica variação aleatória ao raio final
                float finalRadius = _circleRadius * _spiralRadiusGrowth * (1f + Random.Range(-_radiusVariation, _radiusVariation));
                Vector3 finalOffset = new Vector3(
                    Mathf.Cos(finalAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Sin(finalAngle * Mathf.Deg2Rad)
                ) * finalRadius;
                Vector3 targetPosition = origin + finalOffset;

                // Cria a animação espiral
                AnimateSpiral(go.transform, origin, finalAngle, finalRadius, i);

                obj.Activate(origin);
                DebugUtility.Log<CircularZoomOutStrategy>(
                    $"Objeto '{go.name}' spawnado em {origin} movendo-se em espiral para {targetPosition}.",
                    "green",
                    go
                );
            }
        }

        private void AnimateSpiral(Transform target, Vector3 origin, float finalAngle, float finalRadius, int index)
        {
            // Cria uma sequência para combinar animações
            Sequence sequence = DOTween.Sequence();

            // Estado inicial
            float currentRadius = 0f;
            target.position = origin;

            // Configura o Tween para atualizar a posição em espiral
            sequence.Append(DOTween.To(
                () => currentRadius,
                radius =>
                {
                    currentRadius = radius;
                    // Ajusta o ângulo para completar _spiralRotations e terminar em finalAngle
                    float t = radius / finalRadius; // Progresso de 0 a 1
                    float currentAngle = t * _spiralRotations; // Ângulo proporcional ao progresso
                    Vector3 offset = new Vector3(
                        Mathf.Cos((finalAngle + currentAngle) * Mathf.Deg2Rad),
                        0,
                        Mathf.Sin((finalAngle + currentAngle) * Mathf.Deg2Rad)
                    ) * (currentRadius * _spiralRadiusGrowth);
                    target.position = origin + offset;
                },
                finalRadius,
                _animationDuration
            ).SetEase(_easeType));

            // Anima a escala simultaneamente
            sequence.Insert(0, target.DOScale(Vector3.one, _animationDuration).SetEase(_easeType));
        }
    }
}