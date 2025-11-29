using System;
using DG.Tweening;
using UnityEngine;

namespace _ImmersiveGames.Scripts.PlanetSystems.Defense
{
    /// <summary>
    /// Define COMO o minion sai do planeta e chega na órbita.
    /// A estratégia é responsável por:
    /// - posicionar o minion no ponto inicial
    /// - animar até a órbita
    /// - aplicar escala
    /// - respeitar o orbitIdleDelaySeconds (tempo parado em órbita)
    /// - chamar onCompleted no final de tudo (entrada + idle)
    /// </summary>
    public abstract class MinionEntryStrategySo : ScriptableObject
    {
        public abstract Sequence BuildEntrySequence(
            Transform minion,
            Vector3 planetCenter,
            Vector3 orbitPosition,
            Vector3 finalScale,
            float entryDurationSeconds,
            float initialScaleFactor,
            float orbitIdleDelaySeconds,
            Action onCompleted);
    }
}