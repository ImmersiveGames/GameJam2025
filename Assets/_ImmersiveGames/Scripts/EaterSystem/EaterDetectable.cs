using System;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.EaterSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class EaterDetectable : MonoBehaviour, IDetectable
    {
        [SerializeField, Tooltip("Velocidade de movimento do EaterDetectable")]
        private float moveSpeed = 5f;

        [SerializeField, Tooltip("Distância para disparar o evento de comer")]
        private float eatDistance = 5f;

        public event Action<Planets> OnEatPlanet;

        private Transform _cachedTransform;
        private Planets _targetPlanet;

        private void Awake()
        {
            _cachedTransform = transform;
        }

        private void OnEnable()
        {
            GameManager.Instance.OnPlanetMarked += OnPlanetMarked;
            GameManager.Instance.OnPlanetUnmarked += OnPlanetUnmarked;
        }

        private void OnDisable()
        {
            GameManager.Instance.OnPlanetMarked -= OnPlanetMarked;
            GameManager.Instance.OnPlanetUnmarked -= OnPlanetUnmarked;
        }

        private void Update()
        {
            if (_targetPlanet != null && _targetPlanet.IsActive)
            {
                Vector3 direction = (_targetPlanet.transform.position - _cachedTransform.position).normalized;
                _cachedTransform.position += direction * (moveSpeed * Time.deltaTime);

                float distance = Vector3.Distance(
                    new Vector3(_cachedTransform.position.x, 0f, _cachedTransform.position.z),
                    new Vector3(_targetPlanet.transform.position.x, 0f, _targetPlanet.transform.position.z)
                );

                if (distance <= eatDistance)
                {
                    OnEatPlanet?.Invoke(_targetPlanet);
                    DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable está comendo o planeta: {_targetPlanet.name}", "magenta");
                    GameManager.Instance.ClearMarkedPlanet();
                }
            }
        }

        private void OnPlanetMarked(Planets planet)
        {
            _targetPlanet = planet;
            DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable recebeu novo alvo: {planet.name}", "yellow");
        }

        private void OnPlanetUnmarked(Planets planet)
        {
            if (_targetPlanet == planet)
            {
                _targetPlanet = null;
                DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable perdeu alvo: {planet.name}", "red");
            }
        }

        public void OnPlanetDetected(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable detectou planeta: {planet.name}", "green");
        }

        public void OnPlanetLost(Planets planet)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable perdeu planeta: {planet.name}", "red");
        }

        public void OnRecognitionRangeEntered(Planets planet, PlanetResourcesSo resources)
        {
            DebugUtility.LogVerbose<EaterDetectable>($"EaterDetectable reconheceu planeta: {planet.name}, Recursos: {resources}", "blue");
        }
    }
}