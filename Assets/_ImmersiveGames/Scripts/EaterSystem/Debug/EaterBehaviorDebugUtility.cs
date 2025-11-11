using System.Text;
using _ImmersiveGames.Scripts.Utils.DebugSystems;
using _ImmersiveGames.Scripts.PlanetSystems;
using _ImmersiveGames.Scripts.StateMachineSystems;
using UnityEngine;

namespace _ImmersiveGames.Scripts.EaterSystem.Debug
{
    /// <summary>
    /// Utilitário simples de debug para acompanhar o fluxo de estados do Eater.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("ImmersiveGames/Eater/Eater Behavior Debug Utility")]
    [DebugLevel(DebugLevel.Verbose)]
    public sealed class EaterBehaviorDebugUtility : MonoBehaviour
    {
        [Header("Referências")]
        [SerializeField]
        private EaterBehavior behavior;
        [SerializeField]
        private EaterMaster master;

        private readonly StringBuilder _builder = new(256);

        private void Reset()
        {
            behavior = GetComponent<EaterBehavior>();
            master = GetComponent<EaterMaster>();
        }

        private void Awake()
        {
            if (behavior == null)
            {
                behavior = GetComponent<EaterBehavior>();
            }

            if (master == null)
            {
                master = GetComponent<EaterMaster>();
            }
        }

        private void OnEnable()
        {
            if (behavior != null)
            {
                //behavior.EventStateChanged += OnStateChanged;
            }

            if (master != null)
            {
                master.EventStartEatPlanet += OnStartEatPlanet;
                master.EventEndEatPlanet += OnEndEatPlanet;
            }
        }

        private void OnDisable()
        {
            if (behavior != null)
            {
                //behavior.EventStateChanged -= OnStateChanged;
            }

            if (master != null)
            {
                master.EventStartEatPlanet -= OnStartEatPlanet;
                master.EventEndEatPlanet -= OnEndEatPlanet;
            }
        }

        [ContextMenu("Debug/Log Snapshot Atual")]
        private void LogSnapshot()
        {
            if (behavior == null)
            {
                DebugUtility.LogWarning<EaterBehaviorDebugUtility>("Nenhum EaterBehavior configurado.", this);
                return;
            }

            /*var snapshot = behavior.CreateDebugSnapshot();
            if (!snapshot.IsValid)
            {
                DebugUtility.LogWarning<EaterBehaviorDebugUtility>("Snapshot inválido.", this);
                return;
            }

            _builder.Clear();
            _builder.AppendLine("📸 Snapshot do Eater");
            _builder.AppendLine($"- Estado atual: {snapshot.CurrentState}");
            _builder.AppendLine($"- Fome: {snapshot.IsHungry}, Comendo: {snapshot.IsEating}");
            _builder.AppendLine($"- Alvo: {(snapshot.HasTarget ? snapshot.TargetName : "Nenhum")}");
            _builder.AppendLine($"- Contato de proximidade: {snapshot.HasProximityContact}");
            if (snapshot.HasProximityContact)
            {
                _builder.AppendLine($"- Último ponto de contato: {snapshot.LastProximityPoint}");
            }*/

            DebugUtility.Log(_builder.ToString(), instance: this);
        }

        private void OnStateChanged(IState previous, IState current)
        {
            DebugUtility.Log($"🔄 Estado alterado: {previous} → {current}", instance: this);
        }

        private void OnStartEatPlanet(PlanetsMaster planet)
        {
            DebugUtility.Log($"🍽️ Início do consumo: {DescribeTarget(planet)}", instance: this);
        }

        private void OnEndEatPlanet(PlanetsMaster planet)
        {
            DebugUtility.Log($"✅ Fim do consumo: {DescribeTarget(planet)}", instance: this);
        }

        private static string DescribeTarget(PlanetsMaster planet)
        {
            return planet != null ? planet.name : "Desconhecido";
        }
    }
}