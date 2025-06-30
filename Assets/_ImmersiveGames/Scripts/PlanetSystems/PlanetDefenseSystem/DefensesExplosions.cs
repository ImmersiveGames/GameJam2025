using System;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.DetectionsSystems;
using _ImmersiveGames.Scripts.EaterSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem;
using _ImmersiveGames.Scripts.PlayerControllerSystem.ShootingSystem;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;
namespace _ImmersiveGames.Scripts.PlanetSystems.PlanetDefenseSystem
{
    [DebugLevel(DebugLevel.Verbose)]
    public class DefensesExplosions : DeathExplosionEffect
    {
        private IDestructible _destructible;
        private DefensesMaster _defensesMaster;
        private PooledObject _pooledObject;

        protected override void Awake()
        {
            base.Awake();
            TryGetComponent(out _defensesMaster);
        }
        private void OnEnable()
        {
            _defensesMaster.EventDefensesDeath += OnDeath;
        }

        private void OnDisable()
        {
            _defensesMaster.EventDefensesDeath -= OnDeath;
            DisableParticles();
        }
        private void OnDeath()
        {
            DebugUtility.Log<DefensesExplosions>("Imprimindo explosão de defesa", "yellow", this);
            EnableParticles();
        }

        /*protected override void Awake()
        {
            base.Awake();
            if (!TryGetComponent(out _destructible))
            {
                DebugUtility.LogError<DefensesExplosions>($"No IDetectable component found on {gameObject.name}. This component is required for DefensesExplosions to function properly.");
            }
            _defensesMaster.GetComponent<PlayerMaster>();
            if (!TryGetComponent(out _pooledObject))
            {
                DebugUtility.LogError<DefensesExplosions>($"No PooledObject component found on {gameObject.name}. This component is required for DefensesExplosions to function properly.");
            }
        }

        /*rivate void OnEnable()
        {
            _defensesMaster.EventDefensesDeath += OnDeath;
        }

        private void OnDisable()
        {
            _defensesMaster.EventDefensesDeath -= OnDeath;
            DisableParticles();
        }#1#
        private void OnDeath()
        {
            EnableParticles();
            _pooledObject.Deactivate();
        }
        private void OnTriggerEnter(Collider other)
        {
            var destructible = other.GetComponentInParent<IDestructible>();
            if (destructible is null or PlanetHealth or DefensesHealth) return;
            var data = _pooledObject.GetData<ProjectilesData>();
            var actor = _pooledObject.Spawner;
            DebugUtility.Log<DefensesExplosions>($"Entering with destructible: {destructible}, data: {data}, actor: {actor}");
            if (data == null || actor == null) return;
            destructible.TakeDamage(data.damage,actor);
            if(TryGetComponent(out _destructible))
            {
                _destructible.TakeDamage(10, null);
            }
            //_pooledObject.Deactivate();
        }*/
    }
}