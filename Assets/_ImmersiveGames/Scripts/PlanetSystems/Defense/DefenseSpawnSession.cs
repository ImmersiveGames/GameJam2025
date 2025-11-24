using _ImmersiveGames.Scripts.DetectionsSystems.Core;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Representa uma sessão de spawn defensivo de um planeta enquanto houver detectores ativos.
    /// </summary>
    public class DefenseSpawnSession
    {
        public DefenseSpawnSession(IDetector detector, DefenseRole role)
        {
            TriggerDetector = detector;
            ThreatRole = role;
            StartTime = Time.time;
            IsActive = true;
        }

        /// <summary>
        /// Detector que iniciou a sessão.
        /// </summary>
        public IDetector TriggerDetector { get; }

        /// <summary>
        /// Papel identificado para a ameaça.
        /// </summary>
        public DefenseRole ThreatRole { get; }

        /// <summary>
        /// Momento em que a sessão foi iniciada.
        /// </summary>
        public float StartTime { get; }

        /// <summary>
        /// Momento em que a sessão foi encerrada.
        /// </summary>
        public float EndTime { get; private set; }

        /// <summary>
        /// Indica se o ciclo de spawn ainda está ativo.
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Finaliza a sessão de spawn marcando o horário de término.
        /// </summary>
        public void Complete()
        {
            EndTime = Time.time;
            IsActive = false;
        }
    }
}
