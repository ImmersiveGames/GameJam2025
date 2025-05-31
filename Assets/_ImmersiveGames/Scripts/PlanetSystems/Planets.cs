using System;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.Tags;
using UnityEngine;
using _ImmersiveGames.Scripts.Utils.DebugSystems;

namespace _ImmersiveGames.Scripts.PlanetSystems
{
    [DebugLevel(DebugLevel.Verbose)]
    public class Planets : MonoBehaviour, IPlanetInteractable
    {
        private PlanetResourcesSo _resourcesSo;
        private TargetFlag _targetFlag;
        public bool IsActive { get; private set; }
        public event Action<int, PlanetData, PlanetResourcesSo> EventPlanetCreated;
        public event Action<int> EventPlanetDestroyed;
        public event Action<DestructibleObject> OnPlanetDied;
        public void TakeDamage(float damage)
        {
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} recebeu dano de {damage}.", "red");
        }

        private int _planetId;
        private EaterDetectable _eaterDetectable;
        private PlanetData _planetData;

        private void Awake()
        {
            IsActive = true;
            _targetFlag = GetComponentInChildren<TargetFlag>();
            _targetFlag.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            var eater = GameManager.Instance.WorldEater;
            _eaterDetectable = eater.GetComponent<EaterDetectable>();
            if (_eaterDetectable)
            {
                _eaterDetectable.OnEatPlanet += OnEatenByEater;
            }
            GameManager.Instance.OnPlanetMarked += OnMarked;
            GameManager.Instance.OnPlanetUnmarked += OnUnmarked;
        }

        private void OnDisable()
        {
            if (_eaterDetectable != null)
            {
                _eaterDetectable.OnEatPlanet -= OnEatenByEater;
            }
            GameManager.Instance.OnPlanetMarked -= OnMarked;
            GameManager.Instance.OnPlanetUnmarked -= OnUnmarked;
        }

        private void OnEatenByEater(Planets planet)
        {
            if (planet == this)
            {
                DestroyPlanet();
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} foi comido pelo EaterDetectable.", "magenta");
            }
        }

        private void OnMarked(Planets planet)
        {
            if (planet == this)
            {
                _targetFlag.gameObject.SetActive(true);
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} marcado para destruição.", "yellow");
                // Adicionar efeito visual (ex.: outline, partículas)
            }
        }

        private void OnUnmarked(Planets planet)
        {
            if (planet == this)
            {
                _targetFlag.gameObject.SetActive(false);
                DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} desmarcado.", "gray");
                // Remover efeito visual
            }
        }

        public void Initialize(int id, PlanetData data, PlanetResourcesSo resources)
        {
            _planetId = id;
            _planetData = data;
            _resourcesSo = resources;
            EventPlanetCreated?.Invoke(id, data, resources);
        }

        public void ActivateDefenses(IDetectable entity)
        {
            if (!IsActive) return;
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Defesas ativadas em {gameObject.name} para {entityType}", "yellow");
        }

        public void SendRecognitionData(IDetectable entity)
        {
            if (!IsActive) return;
            string entityType = entity.GetType().Name;
            DebugUtility.LogVerbose<Planets>($"Enviando dados de reconhecimento de {gameObject.name} para {entityType}", "cyan");
        }

        public PlanetResourcesSo GetResources()
        {
            return _resourcesSo;
        }

        public void DestroyPlanet()
        {
            IsActive = false;
            EventPlanetDestroyed?.Invoke(_planetId);
            DebugUtility.LogVerbose<Planets>($"Planeta {gameObject.name} destruído.", "red");
        }
    }
}