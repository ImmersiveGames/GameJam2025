using System.Collections;
using _ImmersiveGames.Scripts.ActorSystems;
using _ImmersiveGames.Scripts.AudioSystem.Components;
using _ImmersiveGames.Scripts.Utils.PoolSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.FXSystems
{
    /// <summary>
    /// Controla o ciclo de vida de uma explosão baseada em partículas dentro do pool.
    /// </summary>
    public class ExplosionEffect : PooledObject
    {
        [SerializeField] private ParticleSystem[] particleSystems;
        [SerializeField] private ExplosionAudioController audioController;

        private Coroutine _playingRoutine;

        protected override void OnConfigured(PoolableObjectData config, IActor spawner)
        {
            // Garantir que temos referências às partículas, mesmo se não forem atribuídas manualmente.
            if (particleSystems == null || particleSystems.Length == 0)
            {
                particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            }

            if (audioController == null)
            {
                audioController = GetComponent<ExplosionAudioController>();
            }
        }

        protected override void OnActivated(Vector3 pos, Vector3? direction, IActor spawner)
        {
            if (_playingRoutine != null)
            {
                StopCoroutine(_playingRoutine);
            }

            if (audioController != null)
            {
                audioController.PlayExplosionSound(pos);
            }

            _playingRoutine = StartCoroutine(PlayParticlesRoutine());
        }

        protected override void OnDeactivated()
        {
            if (_playingRoutine != null)
            {
                StopCoroutine(_playingRoutine);
                _playingRoutine = null;
            }

            StopAllParticles();
        }

        protected override void OnReset()
        {
            StopAllParticles();
        }

        protected override void OnReconfigured(PoolableObjectData config)
        {
            // Não há parâmetros dinâmicos além do prefab, então nada a fazer aqui.
        }

        private IEnumerator PlayParticlesRoutine()
        {
            StopAllParticles();
            PlayAllParticles();

            // Aguarda até que todas as partículas tenham finalizado.
            while (AnyParticleAlive())
            {
                yield return null;
            }

            _playingRoutine = null;
            GetPool?.ReturnObject(this);
        }

        private void PlayAllParticles()
        {
            if (particleSystems == null) return;

            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.Clear(true);
                ps.Play(true);
            }
        }

        private void StopAllParticles()
        {
            if (particleSystems == null) return;

            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private bool AnyParticleAlive()
        {
            if (particleSystems == null) return false;

            foreach (var ps in particleSystems)
            {
                if (ps != null && ps.IsAlive(true))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
